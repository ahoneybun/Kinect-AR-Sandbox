﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagDetector.Models;

namespace KiServer.Kinect.ObjectsDetection
{
    public class ObjectDetector
    {
        public List<DetectedObject> FindObjects(Image img)
        {
            List<DetectedObject> objs = new List<DetectedObject>();

            if (img != null)
            {

                //string name = "C:\\temp\\" + DateTime.Now.Ticks + ".png";
                Bitmap bm = new Bitmap(img);
                bm.RotateFlip(RotateFlipType.Rotate180FlipY);
                //bm.Save(name, ImageFormat.Bmp);

                System.Collections.Generic.List<Tag> result = TagDetector.Detector.detectTags(bm, showProcessedInput: false);

                if (result.Count > 0)
                {
                    foreach(Tag r in result)
                    {
                        objs.Add(new DetectedObject(r.Polygon));
                    }
                }
            }

            return objs;
        }

    }
}
