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

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Intermediate storage for the depth data received from the camera
        /// </summary>
        private DepthImagePixel[] depthPixels;
        private byte[] colorPixels;

        private const int DepthWidth = 320; //320
        private const int DepthHeight = 240; //240
        private const int ColorWidth = 1280; //1280
        private const int ColorHeight = 960; //960

        private int fpsController = 0;
        private int FPS_MOD = 1; //30 = 1 por segundo

        private const int MinDepthRange = 800;//mm
        private const int MaxDepthRange = 4000;//mm

        DepthFixer DepthFixer;

        bool EnableFilterHistorical = false;
        bool EnableFilterHolesFilling = false;
        bool EnableFilterAverageMoving = false;
        bool EnableFilterModeMoving = false;
        int MaxFilterHolesFillingDistance = 10;
        int MaxAvgFrames = 4;

        ObjectsDetection.ObjectDetector ObjectDetector = new ObjectDetector();


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

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        public void StartSensor()
        {
            DepthFixer = new DepthFixer(DepthWidth, DepthHeight);
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
            if (DepthWidth == 640)
            {
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            } else {
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            }

            if (ColorWidth == 640)
            {
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            } else
            {
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
            }

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

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    KinectData kd = new KinectData(ColorWidth, ColorHeight);


                    //https://stackoverflow.com/questions/38989837/convert-rgb-array-to-image-in-c-sharp
                    /*
                    System.Windows.Media.Imaging.WriteableBitmap colorBitmap = new System.Windows.Media.Imaging.WriteableBitmap(ColorWidth, ColorHeight, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr32, null);

                    colorBitmap.WritePixels(
                            new System.Windows.Int32Rect(0, 0, ColorWidth, ColorHeight),
                            this.colorPixels,
                            ColorWidth * sizeof(int),
                            0);
                    

                    kd.SetColorData(BitmapFromWriteableBitmap(colorBitmap));
                    */

                    kd.SetColorData(ImageToBitmap(colorFrame));

                    List<DetectedObject> objs = ObjectDetector.FindObjects(kd.ColorImage);
                    kd.SetDetectedObjects(objs);

                    /*
                    System.Drawing.Bitmap B = new System.Drawing.Bitmap(ColorWidth, ColorHeight);
                    int r, g, b, a;
                    int index = 0;
                    for (int y = 0; y < ColorHeight; y++)
                    {
                        for (int x = 0; x < ColorWidth; x++)
                        {
                            r = colorPixels[index++];
                            g = colorPixels[index++];
                            b = colorPixels[index++];
                            a = colorPixels[index++];
                            B.SetPixel(x, y, System.Drawing.Color.FromArgb(a, r, g, b));
                        }
                    }

                    kd.SetColorData(B);
                    */
                    //sender matrix

                    if (Frame != null)// && fpsController == FPS_MOD)
                    {
                        //fpsController = 0; //reset counter
                        Frame(kd, e);
                    }
                }
            }
        }

        public System.Windows.Media.Imaging.BitmapImage ConvertWriteableBitmapToBitmapImage(System.Windows.Media.Imaging.WriteableBitmap wbm)
        {
            System.Windows.Media.Imaging.BitmapImage bmImage = new System.Windows.Media.Imaging.BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                System.Windows.Media.Imaging.PngBitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
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


                    //Console.WriteLine("Depth " + MinDepthRange + "/" + MaxDepthRange + " -> " + this.depthPixels[300].Depth);


                    short[] depth = this.depthPixels.Select(pixel => pixel.Depth).ToArray();
                    short[] depthFixed = DepthFixer.Fix(depth);
                    KinectData kd = new KinectData(DepthWidth, DepthHeight);
                    kd.SetDepthData(depth, depthFixed, MinDepthRange, MaxDepthRange);
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

