using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect.Fix
{
    public class HolesWithHistorical
    {
        private short[] correctionLayer = null;

        public void SetLastCorrectionLayer(short[] data)
        {
            data.CopyTo(correctionLayer, 0);
        }


        public short[] ReplaceHolesWithHistorical(short[] depthArray)
        {
            if (correctionLayer == null)
            {
                correctionLayer = depthArray;
            }

            short[] mergedArray = new short[depthArray.Length];

            for (int index = 0; index < depthArray.Length; index++)
            {
                if (depthArray[index] == 0)
                    mergedArray[index] = correctionLayer[index];
                else
                    mergedArray[index] = depthArray[index];
            }

            return mergedArray;
        }
    }
}
