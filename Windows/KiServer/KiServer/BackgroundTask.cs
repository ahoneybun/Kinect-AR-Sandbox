using KiServer.Kinect;
using KiServer.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace KiServer
{
    public class BackgroundTask
    {

        System.Drawing.Bitmap Gradient;
        KinectController kinectController;
        TCPServer tcpServer = null;
        Thread tcpThread = null;

        //UI Controls
        Image rawCanvas = null;
        Image fixedCanvas = null;
        Image rawColorCanvas = null;
        Image outputCanvasLayer = null;
        Label fpsText = null;

        DateTime fpsTimestamp = DateTime.MinValue;

        System.Drawing.Bitmap bmpOutputLayer = null;
        KinectData currentData = null;

        public bool EnablePreview { get; set; }

        public BackgroundTask()
        {
            EnablePreview = true;
            Gradient = BuildGradient(); //Gradiente para colorear las imagenes de profundidad

            //Controlador para empezar a capturar imagenes de la camara
            kinectController = new KinectController();
            kinectController.Frame += new KinectController.NewImageHandler(NewFrameListener);
        }

        #region UI setters

        public void SetRawColorCanvas(Image canvas)
        {
            rawColorCanvas = canvas;
        }

        public void SetOutputCanvasLayer(Image canvas)
        {
            outputCanvasLayer = canvas;
        }

        public void SetRawCanvas(Image canvas)
        {
            rawCanvas = canvas;
        }

        public void SetFixedCanvas(Image canvas)
        {
            fixedCanvas = canvas;
        }

        public void SetFpsText(Label text)
        {
            fpsText = text;
        }

        #endregion

        #region Filters config

        public void SetFilterHistorical(bool enabled)
        {
            kinectController.SetFilterHistorical(enabled);
        }

        public void SetFilterHolesFilling(bool enabled, int maxDistance = 10)
        {
            kinectController.SetFilterHolesFilling(enabled, maxDistance);
        }

        public void SetFilterAverageMoving(bool enabled, int frames = 1)
        {
            kinectController.SetFilterAverageMoving(enabled, frames);
        }

        public void SetFilterModeMoving(bool enabled, int frames = 1)
        {
            kinectController.SetFilterModeMoving(enabled, frames);
        }

        public void SetObjectDetection(bool enabled)
        {
            kinectController.SetObjectDetection(enabled);
        }


        public void SetDepthRange(short min, short max)
        {
            kinectController.SetDepthRange(min, max);
        }

        #endregion

        #region Control Methods

        //Control methods
        public void Start()
        {
            kinectController.StartSensor();
        }

        public void Stop()
        {
            kinectController.StopSensor();
        }

        public void StartTCP(int port, string ip)
        {
            DataProcessor.IDataProcessor processor = new DataProcessor.GenericProcessor();
            tcpServer = new TCPServer(processor, port, ip);
            tcpThread = new Thread(new ThreadStart(tcpServer.Start));
            tcpThread.Start();
            kinectController.Frame += new KinectController.NewImageHandler(tcpServer.NewFrameListener);
        }

        public void StopTCP()
        {
            if (tcpServer != null)
            {
                kinectController.Frame -= tcpServer.NewFrameListener;
                tcpServer.Stop();
                tcpServer = null;
            }
        }

        public void TakeSnapshot(string folder)
        {
            if (currentData != null)
            {
                string filename = DateTime.Now.ToString("yyyyMMdd-hhmmss");
                currentData.ColorImage.Save(folder + filename + ".bmp");
            }
        }


        //Listener cada vez que se ha obtenido una nueva imagen de la camara
        public void NewFrameListener(KinectData data, EventArgs e)
        {

            if (data.DepthArray != null)
            {
                PrintFPS();
            }

            if (EnablePreview)
            {
                if (data.DepthArray != null)
                {
                    if (fixedCanvas != null) PrintDepthOnCanvas(data.DepthArray, fixedCanvas, data.Width, data.Height, data.MaxDepth);
                    if (rawCanvas != null) PrintDepthOnCanvas(data.RawDepthArray, rawCanvas, data.Width, data.Height, data.MaxDepth);
                }
                if (data.ColorImage != null)
                {
                    currentData = data;
                    if (rawColorCanvas != null) PrintColorOnCanvas(data.ColorImage, rawColorCanvas, data.Width, data.Height);
                }
                PrintOutputCanvasLayer(outputCanvasLayer, data.DetectedObjects, data.Width, data.Height);

            }
        }

        #endregion

        #region Canvas printers

        private void PrintDepthOnCanvas(short[] imageArray, Image canvas, int width, int height, int max)
        {
            try
            {
                System.Windows.Media.Imaging.WriteableBitmap colorBitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr32, null);

                byte[] pixels = new byte[imageArray.Length * 4];
                int pixelsIndex = 0;
                for (int i = 0; i < imageArray.Length; i++)
                {
                    float relativeDepth = Convert.ToInt16(imageArray[i]) / (float)max;
                    System.Drawing.Color c = RelativeDepthToColor(relativeDepth);

                    pixels[pixelsIndex++] = c.B;
                    pixels[pixelsIndex++] = c.G;
                    pixels[pixelsIndex++] = c.R;
                    pixels[pixelsIndex++] = c.A;
                }

                colorBitmap.WritePixels(
                        new System.Windows.Int32Rect(0, 0, width, height),
                        pixels,
                        width * sizeof(int),
                        0);
                colorBitmap.Freeze();

                canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    canvas.Source = colorBitmap;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PrintColorOnCanvas(System.Drawing.Image imageArray, Image canvas, int width, int height)
        {
            try
            {
                canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    canvas.Source = ConvertToBitmapImage(imageArray);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PrintOutputCanvasLayer(Image canvas, List<Kinect.ObjectsDetection.DetectedObject> objects, int width, int height)
        {

            if (objects != null && objects.Count > 0)
            {
                canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    if (bmpOutputLayer == null)
                    {
                        bmpOutputLayer = new System.Drawing.Bitmap(width, height);

                    }
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmpOutputLayer))
                    {
                        g.Clear(System.Drawing.Color.Transparent);
                        System.Drawing.Pen pen1 = new System.Drawing.Pen(System.Drawing.Color.Blue, 4);
                        System.Drawing.Pen pen2 = new System.Drawing.Pen(System.Drawing.Color.Aqua, 4);
                        foreach (Kinect.ObjectsDetection.DetectedObject o in objects)
                        {
                            System.Drawing.Point p1 = new System.Drawing.Point(), p2 = new System.Drawing.Point();
                            for (int c = 0; c < o.RelCorners.Length - 1; c++)
                            {
                                p1 = new System.Drawing.Point(o.RelCorners[c].X * width / 100, o.RelCorners[c].Y * height / 100);
                                p2 = new System.Drawing.Point(o.RelCorners[c + 1].X * width / 100, o.RelCorners[c + 1].Y * height / 100);
                                g.DrawLine(pen1, p1, p2);
                            }
                            p1 = p2;
                            p2 = new System.Drawing.Point(o.RelCorners[0].X * width / 100, o.RelCorners[0].Y * height / 100);
                            g.DrawLine(pen2, p1, p2);

                            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 30);
                            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
                            g.DrawString(o.Data, drawFont, drawBrush, o.RelCenter.X * width / 100, o.RelCenter.Y * height / 100);
                        }
                    }


                    canvas.Source = ConvertToBitmapImage(bmpOutputLayer);
                });
            }
        }

        #endregion

        #region Helpers

        private void PrintFPS()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan dif = now - fpsTimestamp;
            fpsTimestamp = now;

            if (fpsText != null)
            {
                fpsText.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    fpsText.Content = Math.Round(1 / dif.TotalSeconds, 1) + " fps";
                });
            }
        }

        private System.Drawing.Color RelativeDepthToColor(float d)
        {
            System.Drawing.Color c = System.Drawing.Color.Black;
            try
            {
                if (d > 0)
                {
                    c = Gradient.GetPixel(Convert.ToInt32((d > 1 ? 1 : d) * 99), 0);
                }
                else
                {
                    c = System.Drawing.Color.Black;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fallo al seleccionar color " + ex.Message);
            }

            return c;
        }


        private static BitmapImage ConvertToBitmapImage(System.Drawing.Image img)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                img.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); //https://stackoverflow.com/questions/45893536/updating-image-source-from-a-separate-thread-in-wpf

                return bitmapImage;
            }
        }


        private System.Drawing.Bitmap BuildGradient()
        {
            System.Drawing.Bitmap b = new System.Drawing.Bitmap(100, 1);
            //creates the gradient scale which the display is based upon... 
            System.Drawing.Drawing2D.LinearGradientBrush br = new System.Drawing.Drawing2D.LinearGradientBrush(new System.Drawing.RectangleF(0, 0, 100, 5), System.Drawing.Color.Black, System.Drawing.Color.Black, 0, false);
            System.Drawing.Drawing2D.ColorBlend cb = new System.Drawing.Drawing2D.ColorBlend();
            cb.Positions = new[] { 0, 1 / 6f, 2 / 6f, 3 / 6f, 4 / 6f, 5 / 6f, 1 };
            cb.Colors = new[] { System.Drawing.Color.Red, System.Drawing.Color.Orange, System.Drawing.Color.Yellow, System.Drawing.Color.Green, System.Drawing.Color.Blue, System.Drawing.Color.FromArgb(153, 204, 255), System.Drawing.Color.White };
            br.InterpolationColors = cb;

            //puts the gradient scale onto a bitmap which allows for getting a color from pixel
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b);
            g.FillRectangle(br, new System.Drawing.RectangleF(0, 0, b.Width, b.Height));

            return b;
        }

        #endregion
    }

}
