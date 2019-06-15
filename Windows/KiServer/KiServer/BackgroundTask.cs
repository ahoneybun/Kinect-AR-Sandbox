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


        DateTime fpsTimestamp = DateTime.MinValue;
        Label fpsText = null;

        public bool EnablePreview { get; set; }

        public BackgroundTask()
        {
            EnablePreview = true;

            Gradient = BuildGradient(); //Gradiente para colorear las imagenes

            //Controlador para empezar a capturar imagenes de la camara
            kinectController = new KinectController();
            kinectController.Frame += new KinectController.NewImageHandler(NewFrameListener);
        }

        public void SetRawColorCanvas(Image canvas)
        {
            rawColorCanvas = canvas;
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
            /*DataProcessor.IDataProcessor processor = new DataProcessor.GenericProcessor();
            server = new TCPServer(processor);
            server.Start(port, ip);*/

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


        private void PrintDepthOnCanvas(short[] imageArray, Image canvas, int width, int height, int max)
        {
            try
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
                for (int i = 0; i < imageArray.Length; i++)
                {
                    float relativeDepth = Convert.ToInt16(imageArray[i]) / (float)max;
                    int x = i % width;
                    int y = (i - x) / width;

                    bmp.SetPixel(x, y, RelativeDepthToColor(relativeDepth));
                }

                BitmapImage bitmap = ConvertToBitmapImage(bmp);

                canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    canvas.Source = bitmap;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PrintColorOnCanvas(byte[] imageArray, Image canvas, int width, int height)
        {
            try
            {
                WriteableBitmap colorBitmap = new WriteableBitmap(width, height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr32, null);

                colorBitmap.WritePixels(
                        new System.Windows.Int32Rect(0, 0, width, height),
                        imageArray,
                        width * sizeof(int),
                        0);
                colorBitmap.Freeze();

                /*int index = 0;
                int x = 0;
                int y = 0;
                for (int i = 0; i < imageArray.Length; i = i + 4)
                {
                    x = index % width;
                    y = (int)Math.Floor(index / (float)width);

                    System.Drawing.Color c = System.Drawing.Color.FromArgb(imageArray[i + 3], imageArray[i], imageArray[i + 1], imageArray[i + 2]);

                    bmp.SetPixel(x, y, c);
                    index++;
                }

                BitmapImage bitmap = ConvertToBitmapImage(bmp);*/



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
                } else if (data.RawColorArray != null)
                {
                    if (rawColorCanvas != null) PrintColorOnCanvas(data.RawColorArray, rawColorCanvas, data.Width, data.Height); 
                }
            }
        }

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
            //Int16 d255 = Convert.ToInt16(d * 255);
            System.Drawing.Color c = System.Drawing.Color.Black;
            try
            {
                if (d > 0)
                {
                    c = Gradient.GetPixel(Convert.ToInt32((d > 1 ? 1 : d) * 99), 0);
                } else
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


        private static BitmapImage ConvertToBitmapImage(byte[] img)
        {
            BitmapImage biImg = new BitmapImage();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(img);
            biImg.BeginInit();
            biImg.StreamSource = ms;
            biImg.EndInit();

            //System.Windows.Media.ImageSource imgSrc = biImg as System.Windows.Media.ImageSource;

            return biImg;
        }

        private static BitmapImage ConvertToBitmapImage(System.Drawing.Image img)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                img.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
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
    }

}
