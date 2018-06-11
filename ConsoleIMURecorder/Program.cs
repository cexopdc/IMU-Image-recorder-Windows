using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using System.IO;
using System.Diagnostics;

namespace ConsoleIMURecorder
{
    class Program
    {
        static StreamWriter writerCSV;
        static Accelerometer _accelerometer;
        static Gyrometer _gyrometer;
        static GyrometerReading readingGyro;
        static AccelerometerReading readingAccl;

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
            //writerCSV.WriteLine(timeStamp + ","
            //    + readingAccl.AccelerationX + "," + readingAccl.AccelerationY + "," + readingAccl.AccelerationZ);
        }

        static void Main(string[] args)
        {
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
    }
}
