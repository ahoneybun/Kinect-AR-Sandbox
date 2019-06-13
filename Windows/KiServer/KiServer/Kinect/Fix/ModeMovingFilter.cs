using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect.Fix
{
    public class ModeMovingFilter
    {
        private int modeFrameCount;
        private Queue<short[]> modeQueueArray = new Queue<short[]>();
        FixedSizeStack modeQueue;
        Frec[] frecuencies;

        public ModeMovingFilter(int frames, int width, int height)
        {
            modeFrameCount = frames;
            modeQueue = new FixedSizeStack(modeFrameCount);

            frecuencies = new Frec[width * height];

            //inicializamos
            for (int i = 0; i < width * height; i++)
            {
                frecuencies[i] = new Frec(modeFrameCount);
            }
        }

        public class Frec
        {
            public short[] frecs;
            public Frec(int howMany)
            {
                frecs = new short[howMany];
            }

            public short GetMode()
            {
                var groups = frecs.GroupBy(v => v);
                int maxCount = groups.Max(g => g.Count());
                short mode = groups.First(g => g.Count() == maxCount).Key;
                return frecs[0];
            }
        }



        class FixedSizeStack : List<short[]>
        {
            private int MaxNumber;
            public FixedSizeStack(int Limit)
                : base()
            {
                MaxNumber = Limit;
            }

            public void Push(short[] obj)
            {
                if (this.Count == MaxNumber)
                    base.RemoveAt(0);
                base.Add(obj);
            }

        }


        public short[] CreateModeDepthArray(short[] depthArray)
        {
            // This is a method of Weighted Moving Average per pixel coordinate across several frames of depth data.
            // This means that newer frames are linearly weighted heavier than older frames to reduce motion tails,
            // while still having the effect of reducing noise flickering.

            short[] modeDepthArray = new short[depthArray.Length];
            modeQueue.Push(depthArray);


            if (modeQueue.Count < modeFrameCount)
            {
                depthArray.CopyTo(modeDepthArray, 0);
            }
            else
            {
                //recorremos
                for (int q = 0; q < modeFrameCount; q++)
                {

                    for (int i = 0; i < depthArray.Length; i++)
                    {
                        //por cada pixel
                        //coger del 0, 1 y 2...
                        short depth = Convert.ToInt16(modeQueue.ElementAt<short[]>(q)[i] / 10);
                        frecuencies[i].frecs[q] = depth;
                    }
                };
            }

            //resolvemos frecuencias
            for (int i = 0; i < depthArray.Length; i++)
            {
                modeDepthArray[i] = Convert.ToInt16(frecuencies[i].GetMode() * 10 + 5);
            }


            return modeDepthArray;
        }
    }
}
