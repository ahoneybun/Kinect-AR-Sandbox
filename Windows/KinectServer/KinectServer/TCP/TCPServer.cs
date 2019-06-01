using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KinectServer.DataProcessor;
using KinectServer.Kinect;
using Newtonsoft.Json;

namespace KinectServer.TCP
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

        public TCPServer(int port, string ip, IDataProcessor dataProcessor)
        {
            client = null;
            ns = null;

            DataProcessor = dataProcessor;

            //---listen at the specified IP and port no.---
            IPAddress localAdd = IPAddress.Parse(ip);
            listener = new TcpListener(localAdd, port);
            Console.WriteLine("Listening...");
            listener.Start();
        }

        public void Send(KinectData kinectData)
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

                    SendData(ns, data);


                    //Esperamos un ACK del cliente (en este periodo no enviaremos más datos)
                    //el dato devuelto es el Timestamp de la imagen procesada
                    Console.WriteLine("Wait client response...");
                    /*byte[] bytesToRead = new byte[sizeof(int)];
                    ns.Read(bytesToRead, 0, bytesToRead.Length);
                    int response = BitConverter.ToInt32(bytesToRead, 0);*/
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

        /// <summary>
        /// Prepara un bloque de datos y lo envia al cliente
        /// </summary>
        /// <param name="nwStream"></param>
        /// <param name="i"></param>
        private void SendData(NetworkStream nwStream, TCPData data)
        {
            Console.WriteLine("Sending data...");

            
            //Serializamos los datos para enviarlos por TCP
            String dataStr = JsonConvert.SerializeObject(data);
            /*
            byte[] dataBytes = TCPHelpers.StringToByteArray(dataStr);
            byte[] size = BitConverter.GetBytes(dataBytes.Length);

            //Concatena los arrays
            byte[] rv = new byte[size.Length + dataBytes.Length];
            System.Buffer.BlockCopy(size, 0, rv, 0, size.Length);
            System.Buffer.BlockCopy(dataBytes, 0, rv, size.Length, dataBytes.Length);

            //Escribimos en el canal de salida
            nwStream.Write(rv, 0, rv.Length);
            */

            nsWriter.WriteLine(dataStr);
            nsWriter.Flush();
            
            //Console.WriteLine("Sent " + dataBytes.Length + " bytes");

        }
    }
}
