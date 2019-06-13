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

        public DepthFixer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
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

            if (holesFilter != null)
            {
                depthResult = holesFilter.ReplaceHolesWithHistorical(depth);
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

            //guardamos el ultimo array como capa de correccion
            if (holesFilter != null)
            {
                holesFilter.SetLastCorrectionLayer(depthResult);
            }

            return depthResult;
        }






}
}
