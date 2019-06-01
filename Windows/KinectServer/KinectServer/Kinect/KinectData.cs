using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectServer.Kinect
{
    public class KinectData
    {
        public long Timestamp;
        public Microsoft.Kinect.DepthImagePixel[] DepthArray;
        public int DepthWidth;
        public int DepthHeight;
        public int MinDepth;
        public int MaxDepth;

        public KinectData(Microsoft.Kinect.DepthImagePixel[] depthPixels, int depthWidth, int depthHeight, int minDepth, int maxDepth)
        {
            Timestamp = DateTime.UtcNow.Ticks;

            DepthArray = depthPixels;//depthPixels.Select(pixel => pixel.Depth).ToArray();
            DepthWidth = depthWidth;
            DepthHeight = depthHeight;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }
    }
}
