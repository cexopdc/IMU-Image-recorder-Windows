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
        static int BUFFER_SIZE = 4;
        static int bufIndex = 0;
        static bool BUFFER_MODE = true;
        static DataPointViewModel[] dataPointsBuff = new DataPointViewModel[BUFFER_SIZE];
        static long pktCount = 0;
        static long startTime = 0;
        static object bufLock = new System.Object();

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

            /*
            string timeStamp = nanoTime().ToString();
            writerCSV.WriteLine(timeStamp + ","
                + readingGyro.AngularVelocityX + "," + readingGyro.AngularVelocityY + "," + readingGyro.AngularVelocityZ
                + "," + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);
            */

            dataPoint = new DataPointViewModel()
            {
                aX = readingAccl.AccelerationX,
                aY = readingAccl.AccelerationY,
                aZ = readingAccl.AccelerationZ,
                gX = readingGyro.AngularVelocityX,
                gY = readingGyro.AngularVelocityY,
                gZ = readingGyro.AngularVelocityZ
            };

        }

        static private void IMUDataPoll() {
            uint _acclDesiredReportInterval = _accelerometer.MinimumReportInterval;
            uint _gyroDesiredReportInterval = _gyrometer.MinimumReportInterval;

            /*
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
            */

            // Establish the report interval
            _accelerometer.ReportInterval = _acclDesiredReportInterval;
            _gyrometer.ReportInterval = _gyroDesiredReportInterval;


            _accelerometer.ReadingChanged += AcclReadingChanged;
            _gyrometer.ReadingChanged += GyroReadingChanged;
            //Console.WriteLine("Data Collecting ...");
            Console.ReadLine();
            //writerCSV.Close();
        }

        static void Main(string[] args)
        {
            // create a tcp client
            TCPClientIMU.TCPClientIMU client = new TCPClientIMU.TCPClientIMU("127.0.0.1", 9001);

            _accelerometer = Accelerometer.GetDefault(AccelerometerReadingType.Standard);
            _gyrometer = Gyrometer.GetDefault();

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

                            if (_accelerometer == null)
                            { 
                                dataPoint = new DataPointViewModel()
                                {
                                    aX = elapsed.TotalMilliseconds,
                                    aY = rng.NextDouble(),
                                    aZ = rng.NextDouble(),
                                    gX = rng.NextDouble(),
                                    gY = rng.NextDouble(),
                                    gZ = rng.NextDouble()
                                };                
                            }

                            if (BUFFER_MODE)
                            {
                                lock (bufLock)
                                {
                                    if (dataPoint != null)
                                    {
                                        dataPointsBuff[bufIndex] = dataPoint;
                                        bufIndex++;
                                    }
                                }
                                if (bufIndex == BUFFER_SIZE)
                                {
                                    var message = JsonConvert.SerializeObject(dataPointsBuff);
                                    //Console.WriteLine("Sending message {0}", message);
                                    bufIndex = 0;
                                    await client.SendMessageToServerTaskAsync(message);

                                    if (pktCount == 0) startTime = nanoTime();
                                    pktCount++;
                                    //Debug.Log("updateCount: " + updateCount);

                                    if (pktCount % 100 == 0)
                                    {
                                        long currentTime = nanoTime();
                                        double timeElapsed = (currentTime - startTime) / 1000000000.0f;
                                        Console.WriteLine("IMU sending freq: " + 100 / timeElapsed * BUFFER_SIZE);
                                        startTime = currentTime;
                                    }
                                }

                            }
                            else
                            {
                                if (pktCount == 0) startTime = nanoTime();
                                pktCount++;
                                //Debug.Log("updateCount: " + updateCount);

                                if (pktCount % 1000 == 0)
                                {
                                    long currentTime = nanoTime();
                                    double timeElapsed = (currentTime - startTime) / 1000000000.0f;
                                    Console.WriteLine("IMU pkt freq: " + 1000 / timeElapsed);
                                    startTime = currentTime;
                                }

                                var message = JsonConvert.SerializeObject(dataPoint);
                                //Console.WriteLine("Sending message {0}", message);

                                await client.SendMessageToServerTaskAsync(message);
                            }

                            Thread.Sleep(3);
                            
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

