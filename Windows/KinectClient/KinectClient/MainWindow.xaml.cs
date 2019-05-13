using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace KinectClient
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Connect();
        }

        //https://stackoverflow.com/questions/749964/sending-and-receiving-an-image-over-sockets-with-c-sharp

        public void Connect()
        {
            const int PORT_NO = 5000;
            const string SERVER_IP = "127.0.0.1";

            //---data to send to the server---
            string textToSend = DateTime.Now.ToString();

            //---create a TCPClient object at the IP and port no.---
            TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
            NetworkStream nwStream = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(textToSend);

            //---send the text---
            Console.WriteLine("Sending : " + textToSend);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);

            //---read back the text---
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
            Console.ReadLine();
            client.Close();
        }
    }
    
}
