using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectServer.ImagesHelper;

namespace KinectServer.TCP
{

    public class DataListener
    {
        public TCPServer Server { get; set; }

        public void Subscribe(ImageBroker i)
        {
            i.Frame += new ImageBroker.NewImageHandler(NewFrameProcessor);
        }

        public void NewFrameProcessor(Bitmap depthImage, Bitmap colorImage, EventArgs e)
        {
            if (Server != null)
            {
                Server.Send(depthImage, colorImage);
            }
        }
    }
}
