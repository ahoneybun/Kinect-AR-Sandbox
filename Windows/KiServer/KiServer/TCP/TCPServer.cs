using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KiServer.DataProcessor;
using KiServer.Kinect;
using Newtonsoft.Json;

namespace KiServer.TCP
{
    /// <summary>
    /// Servidor TCP (monocliente)
    /// </summary>
    public class TCPServer
    {
        TcpClient client;
        NetworkStream ns;
        IDataProcessor DataProcessor;

        StreamReader nsReader;
        StreamWriter nsWriter;

        //Obtenemos una marca temporal con la que calcularemos los FPS
        DateTime fpsTime = DateTime.MinValue;

        TcpListener listener;

        int port;
        string ip;

        /// <summary>
        /// Prepara y crea el servidor TCP
        /// </summary>
        public TCPServer(IDataProcessor dataProcessor, int port, string ip)
        {
            this.port = port;
            this.ip = ip;
            client = null;
            ns = null;
            DataProcessor = dataProcessor;
        }

        public void Start()
        {
            //---listen at the specified IP and port no.---
            IPAddress localAdd = IPAddress.Parse(ip);
            listener = new TcpListener(localAdd, port);
            Console.WriteLine("Listening...");
            listener.Start();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void NewFrameListener(KinectData depth, EventArgs e)
        {
            Send(depth);
        }


        /// <summary>
        /// Envia datos y espera a que el cliente los procese
        /// </summary>
        private void Send(KinectData kinectData)
        {
            //Si el cliente no está conectado, esperamos a que se conecte
            if (client == null || !client.Connected)
            {
                Console.WriteLine("Awaiting Client ");

                client = listener.AcceptTcpClient();
                ns = client.GetStream();
                nsWriter = new StreamWriter(ns);
                nsReader = new StreamReader(ns);
                    
                nsWriter.AutoFlush = true;

                Console.WriteLine("Client connected ");
            }

            try
            {
                //Creamos el objeto que se enviará
                TCPData data = DataProcessor.GetProcessedData(kinectData);
                data.Timestamp = kinectData.Timestamp;

                //Enviamos el dato
                Console.WriteLine("Sending data...");
                String dataStr = JsonConvert.SerializeObject(data);
                nsWriter.WriteLine(dataStr);
                nsWriter.Flush();

                //Esperamos un ACK del cliente (en este periodo no enviaremos más datos)
                //el dato devuelto es el Timestamp de la imagen procesada
                Console.WriteLine("Wait client response...");
                long processed = Convert.ToInt64(nsReader.ReadLine());

            }

            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message + " (Client Connected? " + client.Connected + ")");
            }
            finally
            {
                ///Calculamos los FPS a los que estamos emitiendo mensajes
                Console.WriteLine("FPS (aprox): " + 1 / (DateTime.UtcNow - fpsTime).TotalSeconds);

                //Obtenemos la siguiente marca temporal con la que calcularemos los FPS
                fpsTime = DateTime.UtcNow;
            }
        }
    }
}
