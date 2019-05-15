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
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json;

namespace KinectClient
{


    class TCPMetadata
    {

    }

    class TCPUpdateData
    {
        public byte[] DepthImage;
    }

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
            byte[] bytesToRead = new byte[1092357];// 819254];// client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, 1092357);// 819254);// client.ReceiveBufferSize);
            Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
            Console.ReadLine();


            string dataStr = Encoding.Default.GetString(bytesToRead, 0, bytesToRead.Length);
            TCPUpdateData data = JsonConvert.DeserializeObject<TCPUpdateData>(dataStr);

            System.Drawing.Image img = byteArrayToImage(data.DepthImage);

            canvas.Source = ConvertToBitmapImage(img);
            //client.Close();
        }


        public static System.Drawing.Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }

        public static BitmapImage ConvertToBitmapImage(System.Drawing.Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }


    }
    
}
