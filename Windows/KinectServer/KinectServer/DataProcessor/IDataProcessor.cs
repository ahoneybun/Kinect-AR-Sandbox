using KinectServer.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectServer.DataProcessor
{
    public interface IDataProcessor
    {
        TCP.TCPData GetProcessedData(KinectData kinectData);
    }
}
