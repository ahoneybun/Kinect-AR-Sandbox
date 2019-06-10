using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KiServer.ImagesHelper
{
    public class ImageBroker
    {

        public void GenerateImageThreadFunction()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(200);
                Console.WriteLine("Generating new image");


                int width = 320, height = 240;

                //bitmap
                Bitmap bmp = new Bitmap(width, height);//, PixelFormat.Format16bppGrayScale);

                //random number
                Random rand = new Random();

                //create random pixels
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        //generate random ARGB value
                        int a = rand.Next(256);
                        int r = rand.Next(256);
                        int g = rand.Next(256);
                        int b = rand.Next(256);

                        Color oc = Color.FromArgb(a, r, g, b);

                        int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                        Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);

                        //set ARGB value
                        bmp.SetPixel(x, y, nc);
                    }
                }



                if (Frame != null)
                {
                    Bitmap bmp16 = ConvertTo16bpp(bmp);
                    Frame(bmp16, bmp16, e);
                }
            }
        }

        public static Bitmap ConvertTo16bpp(Image img)
        {
            var bmp = new Bitmap(img.Width, img.Height,
                          System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
            using (var gr = Graphics.FromImage(bmp))
                gr.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
            return bmp;
        }

        public void ImageFabrik()
        {
            Thread thread = new Thread(new ThreadStart(GenerateImageThreadFunction));
            thread.Start();
        }

        //https://www.codeproject.com/Articles/11541/The-Simplest-C-Events-Example-Imaginable
        public event NewImageHandler Frame;
        public EventArgs e = null;
        public delegate void NewImageHandler(Bitmap depthImage, Bitmap colorImage, EventArgs e);


    }
}
