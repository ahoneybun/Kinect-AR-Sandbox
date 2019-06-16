using KiServer.Kinect.ObjectsDetection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect
{
    public class KinectData
    {
        public long Timestamp;
        public short[] DepthArray;
        public short[] RawDepthArray;
        public System.Drawing.Image ColorImage;
        public int Width;
        public int Height;
        public short MinDepth;
        public short MaxDepth;

        public List<DetectedObject> DetectedObjects;

        public KinectData(int width, int height)
        {
            Timestamp = DateTime.UtcNow.Ticks;
            Width = width;
            Height = height;
        }

        public void SetDepthData(short[] rawDepthPixels, short[] depthPixels, short minDepth, short maxDepth)
        {
            RawDepthArray = rawDepthPixels;
            DepthArray = depthPixels;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }

        public void SetDetectedObjects(List<DetectedObject> objs)
        {
            this.DetectedObjects = objs;
        }

        public void SetColorData(System.Drawing.Image rawColorPixels)
        {
            ColorImage = rawColorPixels;
        }
    }
}
