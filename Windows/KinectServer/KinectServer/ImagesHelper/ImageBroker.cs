using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectServer.ImagesHelper
{
    public class ImageBroker
    {

        public void GenerateImageThreadFunction()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(200);
                Console.WriteLine("Generating new image");


                int width = 20, height = 20;

                //bitmap
                Bitmap bmp = new Bitmap(width, height);

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

                        //set ARGB value
                        bmp.SetPixel(x, y, Color.FromArgb(a, r, g, b));
                    }
                }

                if (Frame != null)
                {
                    Frame(bmp, e);
                }
            }
        }

        public void ImageFabrik()
        {
            Thread thread = new Thread(new ThreadStart(GenerateImageThreadFunction));
            thread.Start();
        }

        //https://www.codeproject.com/Articles/11541/The-Simplest-C-Events-Example-Imaginable
        public event NewImageHandler Frame;
        public EventArgs e = null;
        public delegate void NewImageHandler(Bitmap o, EventArgs e);


    }
}
