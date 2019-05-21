using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectServer.DataProcessor
{
    public class ProcessedData {
        public System.Drawing.Image Image { get; set; }
        public Dictionary<String, String> Metadata { get; set; }
    }

    public interface IDataProcessor
    {
        ProcessedData GetProcessedData(System.Drawing.Bitmap depthImage, System.Drawing.Bitmap colorImage);
    }
}
