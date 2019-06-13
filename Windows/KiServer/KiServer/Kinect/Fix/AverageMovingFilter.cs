using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect.Fix
{
    public class AverageMovingFilter
    {
        private int averageFrameCount;
        private Queue<short[]> averageQueue = new Queue<short[]>();
        private int Width;
        private int Height;

        public AverageMovingFilter(int averageFrameCount, int width, int height)
        {
            this.averageFrameCount = averageFrameCount;
            this.Width = width;
            this.Height = height;
        }

        public short[] CreateAverageDepthArray(short[] depthArray)
        {
            // This is a method of Weighted Moving Average per pixel coordinate across several frames of depth data.
            // This means that newer frames are linearly weighted heavier than older frames to reduce motion tails,
            // while still having the effect of reducing noise flickering.

            averageQueue.Enqueue(depthArray);

            CheckForDequeue();

            int[] sumDepthArray = new int[depthArray.Length];
            short[] averagedDepthArray = new short[depthArray.Length];

            int Denominator = 0;
            int Count = 1;

            // REMEMBER!!! Queue's are FIFO (first in, first out).  This means that when you iterate
            // over them, you will encounter the oldest frame first.

            // We first create a single array, summing all of the pixels of each frame on a weighted basis
            // and determining the denominator that we will be using later.
            foreach (var item in averageQueue)
            {
                // Process each row in parallel
                Parallel.For(0, Height, depthArrayRowIndex =>
                {
                    // Process each pixel in the row
                    for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < Width; depthArrayColumnIndex++)
                    {
                        var index = depthArrayColumnIndex + (depthArrayRowIndex * Width);
                        sumDepthArray[index] += item[index] * Count;
                    }
                });
                Denominator += Count;
                Count++;
            }

            // Once we have summed all of the information on a weighted basis, we can divide each pixel
            // by our calculated denominator to get a weighted average.

            // Process each row in parallel
            Parallel.For(0, Height, depthArrayRowIndex =>
            {
                // Process each pixel in the row
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < Width; depthArrayColumnIndex++)
                {
                    var index = depthArrayColumnIndex + (depthArrayRowIndex * Width);
                    averagedDepthArray[index] = (short)(sumDepthArray[index] / Denominator);
                }
            });

            return averagedDepthArray;
        }

        private void CheckForDequeue(int count = -1)
        {
            int c = count <= 0 ? averageFrameCount : count;
            // We will recursively check to make sure we have Dequeued enough frames.
            // This is due to the fact that a user could constantly be changing the UI element
            // that specifies how many frames to use for averaging.
            if (averageQueue.Count > c)
            {
                averageQueue.Dequeue();
                CheckForDequeue();
            }
        }
    }
}
