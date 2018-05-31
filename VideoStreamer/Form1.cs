using System;
using System.IO;
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
using System.Diagnostics;
using System.Threading;

namespace IMUFrameRecorder
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private Accelerometer _accelerometer;
        private Gyrometer _gyrometer;
        private uint _acclDesiredReportInterval;
        private uint _gyroDesiredReportInterval;
        private string timeStampFolder;
        private StreamWriter writerCSV;
        private bool DEBUG = false;
        private System.Threading.Timer IMUTimer;

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
            //Accelerometer defaultAcc = Accelerometer.GetDefault();
            _accelerometer = Accelerometer.GetDefault(AccelerometerReadingType.Standard); // set to standard instead of linear acceleration!!!
            //Accelerometer tmp = Accelerometer.GetDefault(AccelerometerReadingType.Linear);
            _gyrometer = Gyrometer.GetDefault();
            textBox1.Clear();
            if (_accelerometer != null)
            {
                _acclDesiredReportInterval = _accelerometer.MinimumReportInterval;
                textBox1.Text = "IMUs are available on this device!";
                textBox1.BackColor = Color.Green;
                //InitializeTimer();
            }
            else {              
                textBox1.Text = "No IMU available on this device!";
                textBox1.BackColor = Color.Red;
            }
            if (_gyrometer != null) _gyroDesiredReportInterval = _gyrometer.MinimumReportInterval;
            //if (DEBUG) InitializeTimer();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (videoSource.IsRunning == true)
            {
                videoSource.Stop();
                pictureBox1.Image = null;
                pictureBox1.Invalidate();

                if (_accelerometer != null || DEBUG)
                {
                    // ReadingChanged based
                    _accelerometer.ReadingChanged -= new Windows.Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
                    // Timer based
                    //IMUTimer.Enabled = false;
                    writerCSV.Close();
                    //IMUTimer.Dispose();
                }
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
                            if (
                                videoSource.VideoCapabilities[i].FrameSize.Width > Convert.ToInt32(highestSolution.Split(';')[0]) 
                                && videoSource.VideoCapabilities[i].AverageFrameRate >= 20 
                                && videoSource.VideoCapabilities[i].FrameSize.Height < 500
                                )
                                highestSolution = videoSource.VideoCapabilities[i].FrameSize.Width.ToString() + ";" + i.ToString();
                        }
                        //Set the highest resolution as active
                        videoSource.VideoResolution = videoSource.VideoCapabilities[Convert.ToInt32(highestSolution.Split(';')[1])];
                    }
                }
                catch { }

                Thread threadCamera = new Thread(() => CameraThread())
                {
                    Priority = ThreadPriority.Highest
                };
                threadCamera.Start();

                // set NewFrame event handler
                //videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);

                // make timestamped folder for this record session
                string parentPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                timeStampFolder = nanoTime().ToString();
                string folderPath = System.IO.Path.Combine(parentPath, timeStampFolder);
                System.IO.Directory.CreateDirectory(folderPath);

                videoSource.Start();

                /*****************  IMU handler  ******************************/
                if (_accelerometer != null)
                {
                    // create csv file for this record session
                    String fileName = "imu0.csv";
                    String filePath = System.IO.Path.Combine(folderPath, fileName);
                    writerCSV = new StreamWriter(new FileStream(filePath, FileMode.Create,FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true));
                    writerCSV.WriteLine("timestamp" + "," + "omega_x" + "," + "omega_y" + "," + "omega_z" + "," + "alpha_x" + "," + "alpha_y" + "," + "alpha_z");

                    // Establish the report interval
                    _accelerometer.ReportInterval = _acclDesiredReportInterval;
                    _gyrometer.ReportInterval = _gyroDesiredReportInterval;

                    Thread threadIMU = new Thread(() => IMUThread())
                    {
                        Priority = ThreadPriority.Highest
                    };
                    threadIMU.Start();

               

                    // based on ReadingChanged
                    //_accelerometer.ReadingChanged += new Windows.Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
                    //_accelerometer.ReadingChanged += (s1,e1) =>  ReadingChanged(s1,e1);

                    //_accelerometer.ReadingChanged += 
                    //    (senderObj, evt) =>
                    //    new System.Threading.Thread(() => ReadingChanged(senderObj, evt)).Start();

                    // based on periodical timer
                    //IMUTimer.Enabled = true;
                }
                if (DEBUG)
                {
                    // create csv file for this record session
                    String fileName = "imu0.csv";
                    String filePath = System.IO.Path.Combine(folderPath, fileName);
                    writerCSV = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true));
                    writerCSV.WriteLine("timestamp" + "," + "omega_x" + "," + "omega_y" + "," + "omega_z" + "," + "alpha_x" + "," + "alpha_y" + "," + "alpha_z");

                    //IMUTimer.Enabled = true;
                    InitializeTimer();
                }
            }
        }

    private void CameraThread() {
        // set NewFrame event handler
        videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
    }

    private void IMUThread() {
        _accelerometer.ReadingChanged += new Windows.Foundation.TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(ReadingChanged);
    }



    private void ReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            AccelerometerReading readingAccl = e.Reading;
            GyrometerReading readingGyro = _gyrometer.GetCurrentReading();

            /*
            textBox1.Invoke((Action)delegate
            {
                textBox1.Clear();
                //textBox1.Text = Convert.ToString(_acclDesiredReportInterval);
                textBox1.Text = string.Format("Acceleration - x: {0}, y: {1}, z: {2}", readingAccl.AccelerationX, readingAccl.AccelerationY, readingAccl.AccelerationZ);
                textBox1.AppendText(Environment.NewLine);
                textBox1.AppendText(string.Format("Gyro - x: {0}, y: {1}, z: {2}", readingGyro.AngularVelocityX, readingGyro.AngularVelocityY, readingGyro.AngularVelocityY));
            });
            */
            string timeStamp = nanoTime().ToString();
            writerCSV.WriteLine(timeStamp + "," 
                + readingGyro.AngularVelocityX + "," + readingGyro.AngularVelocityY + "," + readingGyro.AngularVelocityZ 
                + "," + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);
        }

        private void IMUTimer_Tick(object StateObject)
        {
            string timeStamp = nanoTime().ToString();

            if (DEBUG)
            {
                writerCSV.WriteLine(timeStamp + "," + "omega_x" + "," + "omega_y" + "," + "omega_z" + "," + "alpha_x" + "," + "alpha_y" + "," + "alpha_z");
            }
            else
            {
                AccelerometerReading readingAccl = _accelerometer.GetCurrentReading();
                GyrometerReading readingGyro = _gyrometer.GetCurrentReading();

                writerCSV.WriteLine(timeStamp + ","
                    + readingGyro.AngularVelocityX + "," + readingGyro.AngularVelocityY + "," + readingGyro.AngularVelocityZ
                    + "," + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap myImageForBox = (Bitmap)eventArgs.Frame.Clone();
            Bitmap myImageForPNG = (Bitmap)eventArgs.Frame.Clone();
            
            pictureBox1.Image = myImageForBox;

            Thread threadFrameSave = new Thread(() => FrameSaveThread(myImageForPNG))
            {
                Priority = ThreadPriority.Highest
            };
            threadFrameSave.Start();
            
        }

        private void FrameSaveThread(Bitmap myImageForPNG)
        {
            string parentPath = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            string folderPath = System.IO.Path.Combine(parentPath, timeStampFolder);
            string timeStamp = nanoTime().ToString();
            //System.IO.Directory.CreateDirectory(folderPath);
            string fileName = System.IO.Path.Combine(folderPath, timeStamp + ".png");

            myImageForPNG.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource.IsRunning)
            {
                videoSource.Stop();
            }
            if (writerCSV.BaseStream != null) {
                writerCSV.Close();
            }

        }

        private static long nanoTime()
        {
            long nano = Stopwatch.GetTimestamp() * (1000000000L / Stopwatch.Frequency);
            return nano;
        }

        private void InitializeTimer()
        {
            // Call this procedure when the application starts.  
            // Set to 1 second.  
            int timerInterv;
            if (DEBUG) { timerInterv = 1; }
            else
            {
                timerInterv = (int)_acclDesiredReportInterval; // makes the interval minimum
            }
            IMUTimer = new System.Threading.Timer(IMUTimer_Tick, null, 0, timerInterv);

        }


    }
}
