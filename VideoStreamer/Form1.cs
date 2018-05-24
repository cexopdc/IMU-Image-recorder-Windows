﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using AForge.Video;
using AForge.Video.DirectShow;
using Windows.Devices.Sensors;

namespace VideoStreamer
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private Accelerometer _accelerometer;
        private Gyrometer _gyrometer;
        private uint _acclDesiredReportInterval;
        private uint _gyroDesiredReportInterval;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo device in videoDevices)
            {
                comboBox1.Items.Add(device.Name);
            }
            comboBox1.SelectedIndex = 0;

            videoSource = new VideoCaptureDevice();
            _accelerometer = Accelerometer.GetDefault();
            _gyrometer = Gyrometer.GetDefault();
            textBox1.Clear();
            if (_accelerometer != null)
            {
                _acclDesiredReportInterval = _accelerometer.MinimumReportInterval;
                textBox1.Text = "IMUs are available on this device!";
                textBox1.BackColor = Color.Green;
            }
            else {              
                textBox1.Text = "No IMU available on this device!";
                textBox1.BackColor = Color.Red;
            }
            if (_gyrometer != null) _gyroDesiredReportInterval = _gyrometer.MinimumReportInterval;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (videoSource.IsRunning == true)
            {
                videoSource.Stop();
                pictureBox1.Image = null;
                pictureBox1.Invalidate();

                if (_accelerometer != null)
                    _accelerometer.ReadingChanged -= new Windows.Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
            }
            else
            {
                /*****************  Camera handler  ******************************/
                videoSource = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
                try
                {
                    //Check if the video device provides a list of supported resolutions
                    if (videoSource.VideoCapabilities.Length > 0)
                    {
                        string highestSolution = "0;0";
                        //Search for the highest resolution
                        for (int i = 0; i < videoSource.VideoCapabilities.Length; i++)
                        {
                            if (videoSource.VideoCapabilities[i].FrameSize.Width > Convert.ToInt32(highestSolution.Split(';')[0]))
                                highestSolution = videoSource.VideoCapabilities[i].FrameSize.Width.ToString() + ";" + i.ToString();
                        }
                        //Set the highest resolution as active
                        videoSource.VideoResolution = videoSource.VideoCapabilities[Convert.ToInt32(highestSolution.Split(';')[1])];
                    }
                }
                catch { }

                // set NewFrame event handler
                videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
                videoSource.Start();

                /*****************  IMU handler  ******************************/
                if (_accelerometer != null)
                {
                    // Establish the report interval
                    _accelerometer.ReportInterval = _acclDesiredReportInterval;

                    //Window.Current.VisibilityChanged += new WindowVisibilityChangedEventHandler(VisibilityChanged);
                    _accelerometer.ReadingChanged += new Windows.Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);

                    //ScenarioEnableButton.IsEnabled = false;
                    //ScenarioDisableButton.IsEnabled = true;
                }
                else
                {
             
                    //rootPage.NotifyUser("No accelerometer found", NotifyType.StatusMessage);
                }


            }
        }

        private void ReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            AccelerometerReading readingAccl = e.Reading;
            GyrometerReading readingGyro = _gyrometer.GetCurrentReading();
            textBox1.Clear();
            //textBox1.Text = Convert.ToString(_acclDesiredReportInterval);
            textBox1.Text = string.Format("Acceleration - x: {0}, y: {1}, z: {2}", readingAccl.AccelerationX, readingAccl.AccelerationY, readingAccl.AccelerationZ);
            textBox1.AppendText(Environment.NewLine);
            textBox1.AppendText(string.Format("Gyro - x: {0}, y: {1}, z: {2}", readingGyro.AngularVelocityX, readingGyro.AngularVelocityY, readingGyro.AngularVelocityY));
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap myImage = (Bitmap)eventArgs.Frame.Clone();
            pictureBox1.Image = myImage;
            //string strGrabFileName = String.Format("C:\\My_folder\\Snapshot_{0:yyyyMMdd_hhmmss.fff}.bmp", DateTime.Now);
            //myImage.Save(strGrabFileName, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource.IsRunning)
            {
                videoSource.Stop();
            }
        }
    }
}
