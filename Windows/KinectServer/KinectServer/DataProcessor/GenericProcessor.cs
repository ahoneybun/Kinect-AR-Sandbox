using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectServer.DataProcessor
{
    public class GenericProcessor : IDataProcessor
    {
        public ProcessedData GetProcessedData(System.Drawing.Bitmap depthImage, System.Drawing.Bitmap colorImage)
        {
            return new ProcessedData()
            {
                Image = depthImage
            };
        }
    }
}
