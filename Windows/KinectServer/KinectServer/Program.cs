using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;      //required
using System.Net.Sockets;    //required
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using KinectServer.TCP;

namespace KinectServer
{
    class Program
    {
        static void Main(string[] args)
        {

            int PORT_NO = 5000;
            string SERVER_IP = "127.0.0.1";

            //Arrancamos el servidor TCP
            TCPServerController server = new TCPServerController(PORT_NO, SERVER_IP);
            Thread thread = new Thread(new ThreadStart(server.Start));
            thread.Start();


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
        }

    }
}
