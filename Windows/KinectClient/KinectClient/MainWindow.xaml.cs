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
using System.Threading;

namespace KinectClient
{



    class TCPUpdateData
    {
        //public byte[] DepthImage;
        public string Image;
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


            Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
            thread.Start();
            //client.Close();
        }


        public void WorkThreadFunction()
        {
            const int PORT_NO = 5000;
            const string SERVER_IP = "127.0.0.1";
            try
            {
                //---data to send to the server---
                string textToSend = DateTime.Now.ToString();

                //---create a TCPClient object at the IP and port no.---
                TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
                NetworkStream nwStream = client.GetStream();
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(textToSend);

                //---send the text---
                Console.WriteLine("Sending : " + textToSend);
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);

                // do any background work
                while (true)
                {
                    Console.WriteLine("Trying to read");
                    //---read back the text---
                    byte[] sizeBytesToRead = new byte[4];
                    int sizeBytesRead = nwStream.Read(sizeBytesToRead, 0, 4);
                    int size = BitConverter.ToInt32(sizeBytesToRead, 0);

                    byte[] bytesToRead = new byte[size];
                    int bytesRead = nwStream.Read(bytesToRead, 0, size);

                    //byte[] bytesToRead = new byte[1092357];// 819254];// client.ReceiveBufferSize];
                    //int bytesRead = nwStream.Read(bytesToRead, 0, 1092357);// 819254);// client.ReceiveBufferSize);
                    //Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
                    //Console.ReadLine();

                    Console.WriteLine("Trying to decode");

                    string dataStr = Encoding.Default.GetString(bytesToRead, 0, bytesToRead.Length);
                    TCPUpdateData data = JsonConvert.DeserializeObject<TCPUpdateData>(dataStr);

                    Console.WriteLine("Trying to present");
                    
                    
                    System.Drawing.Image img = StringToImage(data.Image);
                    BitmapImage bitmap = ConvertToBitmapImage(img);
                    //canvas.Source.Dispatcher.Invoke(() => canvas.Source = bitmap);
                    canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate()
                    {
                        canvas.Source = bitmap;
                    });
                    
                }
            }
            catch (Exception ex)
            {
                // log errors
                Console.WriteLine("Exception " + ex.Message);
            }
        }


        public static System.Drawing.Image StringToImage(string base64String)
        {
            if (String.IsNullOrWhiteSpace(base64String))
                return null;

            var bytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(bytes);
            return System.Drawing.Image.FromStream(stream);
        }
        /*
        public static System.Drawing.Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }*/

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
                bitmapImage.Freeze(); //https://stackoverflow.com/questions/45893536/updating-image-source-from-a-separate-thread-in-wpf

                return bitmapImage;
            }
        }


    }
    
}
