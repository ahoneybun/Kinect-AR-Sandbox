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
        public int DepthWidth;
        public int DepthHeight;
        public short MinDepth;
        public short MaxDepth;

        public KinectData(short[] rawDepthPixels, short[] depthPixels, int depthWidth, int depthHeight, short minDepth, short maxDepth)
        {
            Timestamp = DateTime.UtcNow.Ticks;

            RawDepthArray = rawDepthPixels;
            DepthArray = depthPixels;//depthPixels.Select(pixel => pixel.Depth).ToArray();
            DepthWidth = depthWidth;
            DepthHeight = depthHeight;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }
    }
}
