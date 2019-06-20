using System;
using System.IO;
using Microsoft.Kinect;

using System.Linq;
using System.Drawing;
using TagDetector;
using TagDetector.Models;
using System.Drawing.Imaging;
using KiServer.Kinect.ObjectsDetection;
using System.Collections.Generic;

namespace KiServer.Kinect
{
    public class KinectController
    {

        //https://www.codeproject.com/Articles/11541/The-Simplest-C-Events-Example-Imaginable
        public event NewImageHandler Frame;
        public EventArgs e = null;
        public delegate void NewImageHandler(KinectData kinectData, EventArgs e);

        private KinectSensor sensor;

        private DepthImagePixel[] depthPixels;
        private byte[] colorPixels;

        private enum DEPTH_RES { PX_320_240, PX_640_480 };
        private enum COLOR_RES { PX_640_480, PX_1280_960 };

        private DEPTH_RES DepthRes = DEPTH_RES.PX_320_240;
        private COLOR_RES ColorRes = COLOR_RES.PX_1280_960;

        private int fpsController = 0;

        private int DepthWidth;
        private int DepthHeight;
        private int ColorWidth;
        private int ColorHeight;

        private short MinDepthRange = 800;//mm
        private short MaxDepthRange = 4000;//mm

        DepthFixer DepthFixer;

        bool EnableFilterHistorical = false;
        bool EnableFilterHolesFilling = false;
        bool EnableFilterAverageMoving = false;
        bool EnableFilterModeMoving = false;
        bool ObjectDetection = false;
        int MaxFilterHolesFillingDistance = 10;
        int MaxAvgFrames = 4;

        ObjectsDetection.ObjectDetector ObjectDetector = new ObjectDetector();


        #region Setters

        public void SetDepthRange(short min, short max)
        {
            MinDepthRange = min;
            MaxDepthRange = max;
            if (DepthFixer != null) DepthFixer.SetDepthRange(min, max);
        }

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

        public void SetObjectDetection(bool enabled)
        {
            ObjectDetection = enabled;
        }

        public void SetFilterModeMoving(bool enabled, int frames = 1)
        {
            EnableFilterModeMoving = enabled;
            MaxAvgFrames = frames;
            SetupFilter();
        }

        private void SetupFilter()
        {
            if (DepthFixer != null)
            {
                DepthFixer.SetModeMovingFilter(EnableFilterModeMoving, MaxAvgFrames);
                DepthFixer.SetHolesWithHistoricalFilter(EnableFilterHistorical);
                DepthFixer.SetClosestPointsFilter(EnableFilterHolesFilling, MaxFilterHolesFillingDistance);
                DepthFixer.SetAverageMovingFilter(EnableFilterAverageMoving, MaxAvgFrames);
            }
        }

        #endregion

        #region Controls

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        public void StartSensor()
        {
            DepthImageFormat depthFormat;
            ColorImageFormat colorFormat;

            switch (DepthRes)
            {
                case DEPTH_RES.PX_640_480:
                    depthFormat = DepthImageFormat.Resolution640x480Fps30;
                    this.DepthWidth = 640;
                    this.DepthHeight = 480;
                    break;
                default:
                    depthFormat = DepthImageFormat.Resolution320x240Fps30;
                    this.DepthWidth = 320;
                    this.DepthHeight = 240;
                    break;
            }

            switch (ColorRes)
            {
                case COLOR_RES.PX_1280_960:
                    colorFormat = ColorImageFormat.RgbResolution1280x960Fps12;
                    this.ColorWidth = 1280;
                    this.ColorHeight = 940;
                    break;
                default:
                    colorFormat = ColorImageFormat.RgbResolution640x480Fps30;
                    this.ColorWidth = 640;
                    this.ColorHeight = 480;
                    break;
            }

            DepthFixer = new DepthFixer(DepthWidth, DepthHeight);
            DepthFixer.SetDepthRange(MinDepthRange, MaxDepthRange);
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

            this.sensor.DepthStream.Enable(depthFormat);
            this.sensor.ColorStream.Enable(colorFormat);

            // Allocate space to put the depth pixels we'll receive
            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
            // Add an event handler to be called whenever there is new depth frame data
            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
            this.sensor.ColorFrameReady += this.SensorColorFrameReady;

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

        #endregion

        #region Helpers

        private Bitmap ImageToBitmap(ColorImageFrame img)
        {
            byte[] pixeldata = new byte[img.PixelDataLength];
            img.CopyPixelDataTo(pixeldata);
            Bitmap bmap = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            System.Drawing.Imaging.BitmapData bmapdata = bmap.LockBits(
                new Rectangle(0, 0, img.Width, img.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(pixeldata, 0, ptr, img.PixelDataLength);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }

        #endregion

        #region FrameReady controller

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    KinectData kd = new KinectData(ColorWidth, ColorHeight);

                    kd.SetColorData(ImageToBitmap(colorFrame));

                    if (ObjectDetection)
                    {
                        List<DetectedObject> objs = ObjectDetector.FindObjects(kd.ColorImage, kd.Width, kd.Height);
                        kd.SetDetectedObjects(objs);
                    }

                    //sender matrix
                    if (Frame != null)
                    {
                        Frame(kd, e);
                    }
                }
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

                    short[] depth = this.depthPixels.Select(pixel => pixel.Depth).ToArray();
                    short[] depthFixed = DepthFixer.Fix(depth);
                    KinectData kd = new KinectData(DepthWidth, DepthHeight);
                    kd.SetDepthData(depth, depthFixed, MinDepthRange, MaxDepthRange);
                    //sender matrix

                    if (Frame != null)
                    {
                        fpsController = 0; //reset counter
                        Frame(kd, e);
                    }

                    fpsController++;
                }
            }
        }

        #endregion

    }
}

