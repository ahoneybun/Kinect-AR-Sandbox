using KiServer.Kinect.Fix;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect
{
    public class DepthFixer
    {
        private AverageMovingFilter avgFilter = null;
        private ClosestPointsFilter closestFilter = null;
        private HolesWithHistorical holesFilter = null;
        private ModeMovingFilter modeFilter = null;

        private int Width;
        private int Height;
        private short MinDepth;//mm
        private short MaxDepth;//mm

        public DepthFixer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public void SetDepthRange(short minDepth, short maxDepth)
        {
            this.MinDepth = minDepth;
            this.MaxDepth = maxDepth;
        }

        public void SetHolesWithHistoricalFilter(bool enabled)
        {
            if (enabled) {
                if (holesFilter == null) holesFilter = new HolesWithHistorical();
            } else {
                holesFilter = null;
            }
        }

        public void SetModeMovingFilter(bool enabled, int frames = 1)
        {
            if (enabled)
            {
                if (modeFilter == null) modeFilter = new ModeMovingFilter(frames, Width, Height);
            }
            else
            {
                modeFilter = null;
            }
        }

        public void SetClosestPointsFilter(bool enabled, int maxDistance = 10)
        {
            if (enabled)
            {
                if (closestFilter == null) closestFilter = new ClosestPointsFilter(maxDistance);
            }
            else
            {
                closestFilter = null;
            }
        }

        public void SetAverageMovingFilter(bool enabled, int frames = 1)
        {
            if (enabled)
            {
                if (avgFilter == null) avgFilter = new AverageMovingFilter(frames, Width, Height);
            }
            else
            {
                avgFilter = null;
            }
        }


        public short[] Fix(short[] depth)
        {
            short[] depthResult = null;

            depth = depth.Select(d => d < MinDepth ? (short)0 : (d > MaxDepth ? Convert.ToInt16(MaxDepth - MinDepth) : Convert.ToInt16(d - MinDepth))).ToArray();
            //depth = depth.Select(d => d < (short)0 ? (short)0 : (d > MaxDepth ? (short)0 : d)).ToArray();

            if (holesFilter != null)
            {
                depthResult = holesFilter.ReplaceHolesWithHistorical(depth);

                //guardamos el ultimo array como capa de correccion
                if (holesFilter != null)
                {
                    holesFilter.SetLastCorrectionLayer(depthResult);
                }
            }

            if (closestFilter != null)
            {
                depthResult = closestFilter.CreateFilteredDepthArray(depthResult != null ? depthResult : depth, Width, Height);
            }

            if (modeFilter != null)
            {
                depthResult = modeFilter.CreateModeDepthArray(depthResult != null ? depthResult : depth);
            }

            if (avgFilter != null)
            {
                depthResult = avgFilter.CreateAverageDepthArray(depthResult != null ? depthResult : depth);
            }

            //si no habia ningun filtro activo
            if (depthResult == null) depthResult = depth;


            return depthResult;
        }






}
}
