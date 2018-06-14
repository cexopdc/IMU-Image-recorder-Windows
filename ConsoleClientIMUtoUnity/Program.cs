using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace ConsoleClientIMUtoUnity
{
    class Program
    {
        static StreamWriter writerCSV;
        static Accelerometer _accelerometer;
        static Gyrometer _gyrometer;
        static GyrometerReading readingGyro;
        static AccelerometerReading readingAccl;
        static DataPointViewModel dataPoint;

        private static long nanoTime()
        {
            long nano = Stopwatch.GetTimestamp() * (1000000000L / Stopwatch.Frequency);
            return nano;
        }

        private static void AcclReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            readingAccl = e.Reading;
            //GyrometerReading readingGyro = _gyrometer.GetCurrentReading();

            //string timeStamp = nanoTime().ToString();
            //writerCSV.WriteLine(timeStamp + ","
            //    + readingGyro.AngularVelocityX + "," + readingGyro.AngularVelocityY + "," + readingGyro.AngularVelocityZ
            //    + "," + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);
            //writerCSV.WriteLine(timeStamp + ","
            //    + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);
        }

        private static void GyroReadingChanged(object sender, GyrometerReadingChangedEventArgs e)
        {
            readingGyro = e.Reading;
            //GyrometerReading readingGyro = _gyrometer.GetCurrentReading();

            string timeStamp = nanoTime().ToString();
            writerCSV.WriteLine(timeStamp + ","
                + readingGyro.AngularVelocityX + "," + readingGyro.AngularVelocityY + "," + readingGyro.AngularVelocityZ
                + "," + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);

            dataPoint = new DataPointViewModel()
            {
                readingAccX = readingAccl.AccelerationX,
                readingAccY = readingAccl.AccelerationY,
                readingAccZ = readingAccl.AccelerationZ,
                readingGyroX = readingGyro.AngularVelocityX,
                readingGyroY = readingGyro.AngularVelocityY,
                readingGyroZ = readingGyro.AngularVelocityZ
            };

        }

        static private void IMUDataPoll() {
            Accelerometer _accelerometer = Accelerometer.GetDefault(AccelerometerReadingType.Standard); ;
            _gyrometer = Gyrometer.GetDefault();
            uint _acclDesiredReportInterval = _accelerometer.MinimumReportInterval;
            uint _gyroDesiredReportInterval = _gyrometer.MinimumReportInterval;

            // make timestamped folder for this record session
            string pathParent = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            string folderPath = System.IO.Path.Combine(pathParent, "IMUData");
            System.IO.Directory.CreateDirectory(new Uri(folderPath).LocalPath);

            // create csv file for this record session
            String fileName = nanoTime() + ".csv";
            String filePath = System.IO.Path.Combine(folderPath, fileName);
            writerCSV = new StreamWriter(new FileStream(new Uri(filePath).LocalPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true));
            writerCSV.WriteLine("timestamp" + "," + "omega_x" + "," + "omega_y" + "," + "omega_z" + "," + "alpha_x" + "," + "alpha_y" + "," + "alpha_z");
            //writerCSV.WriteLine("timestamp" + "," +  "alpha_x" + "," + "alpha_y" + "," + "alpha_z");


            // Establish the report interval
            _accelerometer.ReportInterval = _acclDesiredReportInterval;
            _gyrometer.ReportInterval = _gyroDesiredReportInterval;

            _accelerometer.ReadingChanged += AcclReadingChanged;
            _gyrometer.ReadingChanged += GyroReadingChanged;
            Console.WriteLine("Data Collecting ...");
            Console.ReadLine();
            writerCSV.Close();
        }

        static void Main(string[] args)
        {
            // create a tcp client
            TCPClientIMU.TCPClientIMU client = new TCPClientIMU.TCPClientIMU("127.0.0.1", 9001);



            while (true)
            {
                Task.Run(async () =>
                {
                    await client.ConnectAsync();
                }).Wait();







                Random rng = new Random();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (_accelerometer != null)
                {
                    Thread IMUDataThread = new Thread(() => IMUDataPoll());
                    IMUDataThread.Start();
                }



                try
                {
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            var elapsed = stopwatch.Elapsed;
                            stopwatch.Restart();

                            DataPointViewModel dataPoint = null;
                            if (_accelerometer != null)
                            {
                            /*
                            dataPoint = new DataPointViewModel()
                            {
                                readingAccX = _accelerometer.GetCurrentReading().AccelerationX,
                                readingAccY = _accelerometer.GetCurrentReading().AccelerationY,
                                readingAccZ = _accelerometer.GetCurrentReading().AccelerationZ,
                                readingGyroX = _gyrometer.GetCurrentReading().AngularVelocityX,
                                readingGyroY = _gyrometer.GetCurrentReading().AngularVelocityY,
                                readingGyroZ = _gyrometer.GetCurrentReading().AngularVelocityZ
                            };
                            */
                            }
                            else
                            {
                                dataPoint = new DataPointViewModel()
                                {
                                    readingAccX = elapsed.TotalMilliseconds,
                                    readingAccY = rng.NextDouble(),
                                    readingAccZ = rng.NextDouble(),
                                    readingGyroX = rng.NextDouble(),
                                    readingGyroY = rng.NextDouble(),
                                    readingGyroZ = rng.NextDouble()
                                };
                                Thread.Sleep(5);
                            }

                            var message = JsonConvert.SerializeObject(dataPoint);
                            Console.WriteLine("Sending message {0}", message);

                            await client.SendMessageToServerTaskAsync(message);
                        }
                    }).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}, reconnect...", e);
                }
            }
        }
    }
}

