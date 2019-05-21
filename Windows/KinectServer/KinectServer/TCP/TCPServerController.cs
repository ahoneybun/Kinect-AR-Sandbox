using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using KinectServer.DataProcessor;
using KinectServer.ImagesHelper;
using Newtonsoft.Json;

namespace KinectServer.TCP
{
    /// <summary>
    /// Clase que permite la comunicacion desde el productor de imagenes al servidor TCP
    /// </summary>
    class TCPServerController
    {
        TCPServer Server;
        ImageBroker imageProducer;
        DataListener frameListener;

        public TCPServerController(int port, string ip)
        {
            IDataProcessor processor = new GenericProcessor();

            //Escuchamos y arrancamos el productor de datos
            imageProducer = new ImageBroker();
            frameListener = new DataListener();
            Server = new TCPServer(port, ip, processor);
        }

        public void Start()
        {
            frameListener.Subscribe(imageProducer);
            imageProducer.ImageFabrik();

            //Start TCPServer
            frameListener.Server = Server;
        }




    }
}
