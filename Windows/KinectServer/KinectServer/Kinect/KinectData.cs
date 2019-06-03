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
        public short[] DepthArray;
        public int DepthWidth;
        public int DepthHeight;
        public short MinDepth;
        public short MaxDepth;

        public KinectData(short[] depthPixels, int depthWidth, int depthHeight, short minDepth, short maxDepth)
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
