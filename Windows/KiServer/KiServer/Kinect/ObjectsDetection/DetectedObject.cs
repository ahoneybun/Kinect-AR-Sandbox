using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect.ObjectsDetection
{
    public class RelCoord
    {
        public RelCoord()
        {

        }

        public RelCoord(int x, int y, int containerWidth, int containerHeight)
        {
            //Lo mismo hay que invertirlo 100 - ... en el eje X porque esta volteada la imagen de origen
            this.X = 100 - Convert.ToInt32(x * 100 / containerWidth);
            this.Y = Convert.ToInt32(y * 100 / containerHeight);
        }

        public int X;
        public int Y;
    }

    public class DetectedObject
    {
        private System.Drawing.Point[] Corners;
        private System.Drawing.Point Center;

        public RelCoord[] RelCorners;
        public RelCoord RelCenter;
        public String Data;
        
        public DetectedObject(System.Drawing.Point[] Polygon, int containerWidth, int containerHeight, byte[] data)
        {
            Corners = Polygon;
            Center = GetCenter();

            RelCorners = Corners.Select(c => new RelCoord(c.X, c.Y, containerWidth, containerHeight)).ToArray();
            RelCenter = new RelCoord(Center.X, Center.Y, containerWidth, containerHeight);

            this.Data = new String(data.Select(d => Convert.ToChar(d)).ToArray());
        }


        private System.Drawing.Point GetCenter()
        {
            int totalX = 0, totalY = 0;
            foreach (Point p in Corners)
            {
                totalX += p.X;
                totalY += p.Y;
            }
            int centerX = totalX / Corners.Length;
            int centerY = totalY / Corners.Length;

            return new Point(centerX, centerY);
        }

    }
}
