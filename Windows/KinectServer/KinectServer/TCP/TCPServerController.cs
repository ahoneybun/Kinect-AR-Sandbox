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
        TcpClient client;
        NetworkStream nwStream;
        bool clientConnected;

        TcpListener listener;
        
        int fpsCounter = 0;
        DateTime fpsTime = DateTime.MinValue;

        public TCPServer(int port, string ip)
        {
            client = null;
            nwStream = null;
            clientConnected = false;

            fpsCounter = 0;
            fpsTime = DateTime.MinValue;

            //---listen at the specified IP and port no.---
            IPAddress localAdd = IPAddress.Parse(ip);
            listener = new TcpListener(localAdd, port);
            Console.WriteLine("Listening...");
            listener.Start();
        }
        
        public void Send(Bitmap data)
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
                SendData(nwStream, data);
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

            fpsCounter++;
            if ((DateTime.UtcNow - fpsTime).TotalSeconds >= 1)
            {
                Console.WriteLine("FPS (aprox): " + fpsCounter / (DateTime.UtcNow - fpsTime).TotalSeconds);

                fpsCounter = 0;
                fpsTime = DateTime.MinValue;
            }
        }

        private static void SendData(NetworkStream nwStream, Image i)
        {
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
    }

    class TCPServerController
    {
        TCPServer Server;
        ImageBroker imageProducer;
        FrameListener frameListener;

        public TCPServerController(int port, string ip)
        {
            //Escuchamos y arrancamos el productor de datos
            imageProducer = new ImageBroker();
            frameListener = new FrameListener();
            Server = new TCPServer(port, ip);
        }

        public void Start()
        {
            frameListener.Subscribe(imageProducer);
            imageProducer.ImageFabrik();

            //Start TCPServer
            frameListener.Server = Server;
        }


        public class FrameListener
        {
            public TCPServer Server { get; set; }

            public void Subscribe(ImageBroker i)
            {
                i.Frame += new ImageBroker.NewImageHandler(NewFrameProcessor);
            }

            public void NewFrameProcessor(Bitmap data, EventArgs e)
            {
                if (Server != null)
                {
                    Server.Send(data);
                }
            }
        }


    }
}
