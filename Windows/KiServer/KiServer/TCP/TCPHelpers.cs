using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.TCP
{
    class TCPHelpers
    {
        public static byte[] StringToByteArray(string str)
        {
            Encoding encoding = Encoding.Default;
            return encoding.GetBytes(str);
        }

        public static System.Drawing.Image ByteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }

        public static byte[] ImageToByteArray(Image img)
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }

        public static string ImageToString(Image image)
        {
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, System.Drawing.Imaging.ImageFormat.Bmp); //RawFormat
                byte[] imageBytes = m.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
            /*

            if (image == null) return String.Empty;

            var stream = new MemoryStream();
            image.Save(stream, image.RawFormat);
            var bytes = stream.ToArray();

            return Convert.ToBase64String(bytes);*/
        }

        public static Image StringToImage(string base64String)
        {
            if (String.IsNullOrWhiteSpace(base64String))
                return null;

            var bytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(bytes);
            return Image.FromStream(stream);
        }
    }
}
