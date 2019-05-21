using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KinectServer.ImagesHelper;
using Newtonsoft.Json;

namespace KinectServer.TCP
{
    class TCPServer
    {
        int PORT_NO;
        string SERVER_IP;


        public TCPServer(int port, string ip)
        {
            PORT_NO = port;
            SERVER_IP = ip;
        }

        public void ListenLoop()
        {



            //---listen at the specified IP and port no.---
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");
            listener.Start();

            TcpClient client = null;
            NetworkStream nwStream = null;
            bool clientConnected = false;
            int counter = 0;

            int fpsCounter = 0;
            DateTime fpsTime = DateTime.MinValue;

            //ImageBroker imgBroker = new ImageBroker();

            while (true)
            {
                if (fpsTime == DateTime.MinValue) fpsTime = DateTime.UtcNow;

                if (!clientConnected)
                {
                    Console.WriteLine("Awaiting Client ");
                    client = listener.AcceptTcpClient();
                    nwStream = client.GetStream();
                    clientConnected = true;
                    Console.WriteLine("Client connected ");
                }

                try
                {
                    Console.WriteLine("Sending back : " + counter);


                    Image i = ImageBroker.GetImage();

                    byte[] ibytes = TCPHelpers.ImageToByteArray(i);

                    TCPUpdateData data = new TCPUpdateData()
                    {
                        DepthImage = ibytes
                    };

                    String dataStr = JsonConvert.SerializeObject(data);
                    byte[] dataBytes = TCPHelpers.StringToByteArray(dataStr);

                    nwStream.Write(dataBytes, 0, dataBytes.Length);

                    Console.WriteLine("Sent " + dataBytes.Length + " bytes");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message + " (Client Connected? " + client.Connected + ")");
                    clientConnected = client.Connected;
                }
                finally
                {
                    Console.WriteLine("Sleep...");
                    //System.Threading.Thread.Sleep(1000);
                }

                //nwStream.Write(ibytes, 0, ibytes.Length);

                counter++;

                fpsCounter++;
                if ((DateTime.UtcNow - fpsTime).TotalSeconds >= 1)
                {
                    Console.WriteLine("FPS (aprox): " + fpsCounter);

                    fpsCounter = 0;
                    fpsTime = DateTime.MinValue;
                }
            }
        }
    }
}
