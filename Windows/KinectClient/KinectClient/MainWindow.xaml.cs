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
        const string KEY_DEPTH_RAW_ARRAY = "RawDepthArray";
        const string KEY_DEPTH_ARRAY = "DepthArray";
        const string KEY_DEPTH_WIDTH = "DepthWidth";
        const string KEY_DEPTH_HEIGHT = "DepthHeight";
        const string KEY_DEPTH_MIN = "MinDepth";
        const string KEY_DEPTH_MAX = "MaxDepth";

        public long Timestamp;
        public Dictionary<string, string> Metadata;

        public int W;
        public int H;

        public int MAX;
        public int MIN;


        public float[,] GetRelativeDepths(Dictionary<string, string> Metadata, bool getRaw = false)
        {
            W = Convert.ToInt32(Metadata[KEY_DEPTH_WIDTH]);
            H = Convert.ToInt32(Metadata[KEY_DEPTH_HEIGHT]);
            MAX = Convert.ToInt16(Metadata[KEY_DEPTH_MAX]);
            MIN = Convert.ToInt16(Metadata[KEY_DEPTH_MIN]);

            int Range = MAX - MIN;

            string depthsString;

            if (getRaw) depthsString = Base64Decode(Metadata[KEY_DEPTH_RAW_ARRAY]);
            else depthsString = Base64Decode(Metadata[KEY_DEPTH_ARRAY]);

            float[,] heights = new float[W, H];
            int x = 0;
            int y = -1;
            for (int i = 0; i < depthsString.Length; i++)
            {

                //float height = 1 - (Convert.ToInt16(depthsString[i]) - MIN) / (float)Range;

                float height = Convert.ToInt16(depthsString[i]) / (float)MAX;

                if (height > 1)
                {
                    Console.WriteLine("what");
                }

                if (i % W == 0)
                {
                    x = 0;
                    y++;
                }
                else
                {
                    x++;
                }

                //set value
                heights[x, y] = height;
            }

            return heights;
        }
        /*
        public short[] ToDepth()
        {

            W = Convert.ToInt32(Metadata[KEY_DEPTH_WIDTH]);
            H = Convert.ToInt32(Metadata[KEY_DEPTH_HEIGHT]);
            MAX = Convert.ToInt32(Metadata[KEY_DEPTH_MAX]);

            string depthsString = Base64Decode(Metadata[KEY_DEPTH_ARRAY]);

            //convertimos la searializacion string en un array de shorts
            short[] depths = depthsString.Select(character => Convert.ToInt16(character)).ToArray();
            //int[] depths = depthsString.Select(character => (int)Char.GetNumericValue(character)).ToArray();
            
            return depths;
        }
        */
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

                        Console.WriteLine("Trying to decode");
                        TCPData data = JsonConvert.DeserializeObject<TCPData>(dataStr);

                    try
                    {
                        Console.WriteLine("Trying to parse");
                        float[,] depths = data.GetRelativeDepths(data.Metadata);
                        float[,] rawdepths = data.GetRelativeDepths(data.Metadata, true);

                        Console.WriteLine("Trying to present");
                        Bitmap bmp = new Bitmap(data.W, data.H);
                        Bitmap rawbmp = new Bitmap(data.W, data.H);

                        for (int xi = 0; xi < depths.GetLength(0); xi++)
                        {
                            for (int yi = 0; yi < depths.GetLength(1); yi++)
                            {
                                float rel = depths[xi, yi];
                                Int16 grey = Convert.ToInt16(rel * 255);
                                System.Drawing.Color nc = System.Drawing.Color.FromArgb(255, grey, grey, grey);
                                bmp.SetPixel(xi, yi, nc);

                                //raw data
                                float rawrel = rawdepths[xi, yi];
                                Int16 rawgrey = Convert.ToInt16(rawrel * 255);
                                System.Drawing.Color rawnc = System.Drawing.Color.FromArgb(255, rawgrey, rawgrey, rawgrey);
                                rawbmp.SetPixel(xi, yi, rawnc);
                            }
                        }

                        BitmapImage bitmap = ConvertToBitmapImage(bmp);
                        BitmapImage rawbitmap = ConvertToBitmapImage(rawbmp);


                        //canvas.Source.Dispatcher.Invoke(() => canvas.Source = bitmap);
                        canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                        {
                            canvas.Source = bitmap;
                        });

                        rawcanvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
                        {
                            rawcanvas.Source = rawbitmap;
                        });


                        //Escribimos en el canal de salida

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception " + ex.Message);
                    }

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
