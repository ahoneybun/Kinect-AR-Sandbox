using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;      //required
using System.Net.Sockets;    //required
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace KinectServer
{
    class TCPMetadata
    {

    }

    class TCPUpdateData
    {
        public byte[] DepthImage; 
    }


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
            /*byte[] buffer = new byte[client.ReceiveBufferSize];
            
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

                //Image img = byteArrayToImage(ibytes);


                TCPUpdateData data = new TCPUpdateData()
                {
                    DepthImage = ibytes
                };

                String dataStr = JsonConvert.SerializeObject(data);
                byte[] dataBytes = ToByteArray(dataStr);

                nwStream.Write(dataBytes, 0, dataBytes.Length);
                //nwStream.Write(ibytes, 0, ibytes.Length);

                counter++;
            }
        }

        public static byte[] ToByteArray(string str)
        {
            Encoding encoding = Encoding.Default;
            return encoding.GetBytes(str);
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


        public static System.Drawing.Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }

        public static byte[] ImageToByte(Image img)
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }
    }
}
