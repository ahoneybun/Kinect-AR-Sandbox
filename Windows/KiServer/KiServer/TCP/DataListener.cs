using System;
using KiServer.Kinect;
using Microsoft.Kinect;

namespace KiServer.TCP
{

    public class DataListener
    {
        public TCPServer Server { get; set; }

        public void Subscribe(KinectController i)
        {
            i.Frame += new KinectController.NewImageHandler(NewFrameProcessor);
        }

        public void NewFrameProcessor(KinectData depth, EventArgs e)
        {
            if (Server != null)
            {
                //Server.Send(depth);
            }
        }
    }
}
