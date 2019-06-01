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
using System.Drawing;

namespace KinectClient
{
    class TCPData
    {
        public long Timestamp;
        public Dictionary<string, string> Metadata;

        public int W;
        public int H;

        public int MAX;
        public int MIN;

        public short[] ToDepth()
        {
            const string KEY_DEPTH_ARRAY = "DepthArray";
            const string KEY_DEPTH_WIDTH = "DepthWidth";
            const string KEY_DEPTH_HEIGHT = "DepthHeight";
            const string KEY_DEPTH_MIN = "MinDepth";
            const string KEY_DEPTH_MAX = "MaxDepth";

            W = Convert.ToInt32(Metadata[KEY_DEPTH_WIDTH]);
            H = Convert.ToInt32(Metadata[KEY_DEPTH_HEIGHT]);
            MAX = Convert.ToInt32(Metadata[KEY_DEPTH_MAX]);

            string depthsString = Base64Decode(Metadata[KEY_DEPTH_ARRAY]);

            //convertimos la searializacion string en un array de shorts
            short[] depths = depthsString.Select(character => Convert.ToInt16(character)).ToArray();
            //int[] depths = depthsString.Select(character => (int)Char.GetNumericValue(character)).ToArray();
            
            return depths;
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
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

                //---create a TCPClient object at the IP and port no.---
                TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
                NetworkStream ns = client.GetStream();

                StreamReader nsReader = new StreamReader(ns);
                StreamWriter nsWriter = new StreamWriter(ns);
                ns.Flush();
                nsWriter.AutoFlush = true;

                //---send the text---
                //Console.WriteLine("Sending : " + textToSend);
                //ns.Write(BitConverter.GetBytes(textToSend), 0, BitConverter.GetBytes(textToSend).Length);

                // do any background work
                while (true)
                {
                    Console.WriteLine("Trying to read");


                    string dataStr = nsReader.ReadLine();
                    /*
                    //---read back the text---
                    byte[] sizeBytesToRead = new byte[4];
                    int sizeBytesRead = ns.Read(sizeBytesToRead, 0, 4);
                    int size = BitConverter.ToInt32(sizeBytesToRead, 0);

                    byte[] bytesToRead = new byte[size];
                    int bytesRead = ns.Read(bytesToRead, 0, size);
                    */



                    //byte[] bytesToRead = new byte[1092357];// 819254];// client.ReceiveBufferSize];
                    //int bytesRead = nwStream.Read(bytesToRead, 0, 1092357);// 819254);// client.ReceiveBufferSize);
                    //Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
                    //Console.ReadLine();

                    Console.WriteLine("Trying to decode");

                    //string dataStr = Encoding.Default.GetString(bytesToRead, 0, bytesToRead.Length);
                    TCPData data = JsonConvert.DeserializeObject<TCPData>(dataStr);

                    Console.WriteLine("Trying to parse");

                    short[] depths = data.ToDepth();


                    Console.WriteLine("Trying to present");

                    /*
                    WriteableBitmap bitmap = new WriteableBitmap(data.W, data.H, 96.0, 96.0, PixelFormats.Bgr32, null);
                    bitmap.WritePixels(
                        new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                        depths,
                        bitmap.PixelWidth * sizeof(int),
                        0);
                    bitmap.Freeze();
                    */

                    
                    Bitmap bmp = new Bitmap(data.W, data.H);

                    int x = 0;
                    int y = -1;
                    for (int i = 0; i < depths.Length; i++)
                    {
                        int a = Convert.ToInt16((depths[i] / (float)data.MAX) * 255);
                        System.Drawing.Color nc = System.Drawing.Color.FromArgb(255, a, a, a);
                        
                    
                        if (i % data.W == 0)
                        {
                            x = 0;
                            y++;
                        } else
                        {
                            x++;
                        }

                        //set ARGB value
                        bmp.SetPixel(x, y, nc);

                    }
                    

                    

                    BitmapImage bitmap = ConvertToBitmapImage(bmp);

                    
                    //canvas.Source.Dispatcher.Invoke(() => canvas.Source = bitmap);
                    canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        canvas.Source = bitmap;
                    });


                    //Escribimos en el canal de salida


                    Console.WriteLine("Response to server " + data.Timestamp);
                    //ns.Write(BitConverter.GetBytes(data.Timestamp), 0, sizeof(int));
                    nsWriter.WriteLine(data.Timestamp.ToString());
                    nsWriter.Flush();

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
