using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;      //required
using System.Net.Sockets;    //required
using System.Drawing;

namespace KinectServer
{
    class Program
    {
        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";

        static void Main(string[] args)
        {
            //---listen at the specified IP and port no.---
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");
            listener.Start();

            //---incoming client connected---
            TcpClient client = listener.AcceptTcpClient();

            //---get the incoming data through a network stream---
            NetworkStream nwStream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            /*
            //---read incoming stream---
            int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

            //---convert the data received into a string---
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Received : " + dataReceived);

            //---write back the text to the client---
            Console.WriteLine("Sending back : " + dataReceived);
            nwStream.Write(buffer, 0, bytesRead);
            client.Close();
            listener.Stop();
            Console.ReadLine();
            */

            int counter = 0;
            while(true)
            {
                
                System.Threading.Thread.Sleep(5000);
                Console.WriteLine("Sending back : " + counter);

                Image i = RandomImage();
                byte[] ibytes = ImageToByte(i);

                nwStream.Write(ibytes, 0, ibytes.Length);

                counter++;
            }
        }


        private static Bitmap RandomImage()
        {
            int width = 640, height = 320;

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

            return bmp;
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}
