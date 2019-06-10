using System;
using System.IO;
using Microsoft.Kinect;

using System.Linq;
namespace KiServer.Kinect
{
    public class KinectController
    {

        //https://www.codeproject.com/Articles/11541/The-Simplest-C-Events-Example-Imaginable
        public event NewImageHandler Frame;
        public EventArgs e = null;
        public delegate void NewImageHandler(KinectData kinectData, EventArgs e);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;

        private const int Width = 320; //320
        private const int Height = 240; //240

        private int fpsController = 0;
        private int FPS_MOD = 1; //30 = 1 por segundo

        private const int MinDepthRange = 800;//mm
        private const int MaxDepthRange = 4000;//mm

        DepthFixer DepthFixer;

        bool EnableFilterHistorical = false;
        bool EnableFilterHolesFilling = false;
        bool EnableFilterAverageMoving = false;
        int MaxFilterHolesFillingDistance = 10;
        int MaxAvgFrames = 4;



        public void SetFilterHistorical(bool enabled)
        {
            EnableFilterHistorical = enabled;
            SetupFilter();
        }

        public void SetFilterHolesFilling(bool enabled, int maxDistance = 10)
        {
            EnableFilterHolesFilling = enabled;
            MaxFilterHolesFillingDistance = maxDistance;
            SetupFilter();
        }

        public void SetFilterAverageMoving(bool enabled, int frames = 1)
        {
            EnableFilterAverageMoving = enabled;
            MaxAvgFrames = frames;
            SetupFilter();
        }

        private void SetupFilter()
        {
            if (DepthFixer != null)
            {
                DepthFixer.SetFilterHistorical(EnableFilterHistorical);
                DepthFixer.SetFilterHolesFilling(EnableFilterHolesFilling, MaxFilterHolesFillingDistance);
                DepthFixer.SetFilterAverageMoving(EnableFilterAverageMoving, MaxAvgFrames);
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        public void StartSensor()
        {
            DepthFixer = new DepthFixer(Width, Height);
            SetupFilter();


            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            //this.sensor.DepthStream.Range = DepthRange.Near;
            //this.sensor.DepthStream.Range = DepthRange.Default;

            // Turn on the depth stream to receive depth frames
            if (Width == 640)
            {
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            } else {
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            }

            // Allocate space to put the depth pixels we'll receive
            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

            // Add an event handler to be called whenever there is new depth frame data
            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;

            // Start the sensor!
            try
            {
                this.sensor.Start();
            }
            catch (IOException)
            {
                this.sensor = null;
            }
        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        public void StopSensor()
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                    // Get the min and max reliable depth for the current frame
                    //int minDepth = depthFrame.MinDepth;
                    //int maxDepth = depthFrame.MaxDepth;


                    Console.WriteLine("Depth " + MinDepthRange + "/" + MaxDepthRange + " -> " + this.depthPixels[300].Depth);


                    short[] depth = this.depthPixels.Select(pixel => pixel.Depth).ToArray();
                    short[] depthFixed = DepthFixer.Fix(depth);
                    KinectData kd = new KinectData(depth, depthFixed, Width, Height, MinDepthRange, MaxDepthRange);

                    //sender matrix

                    if (Frame != null && fpsController == FPS_MOD)
                    {
                        fpsController = 0; //reset counter
                        Frame(kd, e);
                    }

                    fpsController++;
                }
            }
        }
        /*
        static short[,] ConvertMatrix(DepthImagePixel[] flat, int m, int n)//double[] flat, int m, int n)
        {
            if (flat.Length != m * n)
            {
                throw new ArgumentException("Invalid length");
            }
            
            short[,] ret = new short[m, n];

            // BlockCopy uses byte lengths: a double is 8 bytes
            Buffer.BlockCopy(flat.get, 0, ret, 0, flat.Length * sizeof(short));
            return ret;
        }*/

    }
}

