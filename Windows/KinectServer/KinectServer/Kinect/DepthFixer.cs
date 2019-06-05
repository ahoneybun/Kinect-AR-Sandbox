using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectServer.Kinect
{
    public class DepthFixer
    {
        // Will specify how many non-zero pixels within a 1 pixel band
        // around the origin there should be before a filter is applied
        private int foundValuesThreshold;

        // Will specify how many frames to hold in the Queue for averaging
        private int averageFrameCount;

        // The actual Queue that will hold all of the frames to be averaged
        private Queue<short[]> averageQueue = new Queue<short[]>();

        private bool enableFilter = false;
        private bool enableAverage = false;

        public DepthFixer(bool enableFilter, bool enableAverage, int foundValuesThreshold, int averageFrameCount)
        {
            this.foundValuesThreshold = foundValuesThreshold;
            this.averageFrameCount = averageFrameCount;

            this.enableFilter = enableFilter;
            this.enableAverage = enableAverage;
        }


        public short[] Fix(short[] depth, int width, int height)
        {
            short[] depthResult = null;

            if (this.enableFilter)
            {
                depthResult = CreateFilteredDepthArray(depth, width, height);
            }

            if (this.enableAverage)
            {
                depthResult = CreateAverageDepthArray(depthResult);
            }

            return depthResult;
        }


        private short[] CreateAverageDepthArray(short[] depthArray)
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
                Parallel.For(0, 240, depthArrayRowIndex =>
                {
                    // Process each pixel in the row
                    for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < 320; depthArrayColumnIndex++)
                    {
                        var index = depthArrayColumnIndex + (depthArrayRowIndex * 320);
                        sumDepthArray[index] += item[index] * Count;
                    }
                });
                Denominator += Count;
                Count++;
            }

            // Once we have summed all of the information on a weighted basis, we can divide each pixel
            // by our calculated denominator to get a weighted average.

            // Process each row in parallel
            Parallel.For(0, 240, depthArrayRowIndex =>
            {
                // Process each pixel in the row
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < 320; depthArrayColumnIndex++)
                {
                    var index = depthArrayColumnIndex + (depthArrayRowIndex * 320);
                    averagedDepthArray[index] = (short)(sumDepthArray[index] / Denominator);
                }
            });

            return averagedDepthArray;
        }

        private void CheckForDequeue()
        {
            // We will recursively check to make sure we have Dequeued enough frames.
            // This is due to the fact that a user could constantly be changing the UI element
            // that specifies how many frames to use for averaging.
            if (averageQueue.Count > averageFrameCount)
            {
                averageQueue.Dequeue();
                CheckForDequeue();
            }
        }

        public class BandCoordinate
        {
            public int X;
            public int Y;
        }
        /*
        private List<BandCoordinate> GetBandCoordinates(int x, int y, int radius, bool includeInner = false)
        {
            List<BandCoordinate> coords = new List<BandCoordinate>();

            BandCoordinate TopLeft = new BandCoordinate() { X = x - radius, Y = y - radius };
            BandCoordinate BottomRight = new BandCoordinate() { X = x + radius, Y = y + radius };

            //si queremos incluir tambien los cuadrados interiores
            if (includeInner)
            {
                for (int yi = y - radius; yi <= (y + radius); yi++)
                {
                    for (int xi = x - radius; xi <= (x + radius); xi++)
                    {
                        if (!(xi == x && yi == y)) //ignoramos el centro siempre
                        {
                            coords.Add(new BandCoordinate() { X = xi, Y = yi });
                        }
                    }
                }
            } else
            {
                //si solo queremos el borde conforme al radio que hemos definido

                //añadimos los bordes superior e inferior
                for (int xi = TopLeft.X; xi <= BottomRight.X; xi++)
                {
                    coords.Add(new BandCoordinate() { X = xi, Y = TopLeft.Y });
                    coords.Add(new BandCoordinate() { X = xi, Y = BottomRight.Y });
                }

                //añadimos los bordes izquierdo y derecho evitando los extremos (que ya hemos añadido anteriormente)
                for (int yi = TopLeft.Y + 1; yi < BottomRight.Y; yi++)
                {
                    coords.Add(new BandCoordinate() { X = TopLeft.X, Y = yi });
                    coords.Add(new BandCoordinate() { X = BottomRight.X, Y = yi });
                }
            }

            return coords;
        }*/

        private short[] CreateFilteredDepthArray(short[] depthArray, int width, int height)
        {
            /////////////////////////////////////////////////////////////////////////////////////
            // based on this Codeplex Project https://www.codeproject.com/Articles/317974/KinectDepthSmoothing
            /////////////////////////////////////////////////////////////////////////////////////

            short[] smoothDepthArray = new short[depthArray.Length];

            // We will be using these numbers for constraints on indexes
            int widthBound = width - 1;
            int heightBound = height - 1;

            // We process each row

            Parallel.For(0, height, depthArrayRowIndex =>
            //for (int depthArrayRowIndex = 0; depthArrayRowIndex < height; depthArrayRowIndex++)
            {

                // Process each pixel in the row
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < width; depthArrayColumnIndex++)
                {
                    var depthIndex = depthArrayColumnIndex + (depthArrayRowIndex * width);

                    // We are only concerned with eliminating 'white' noise from the data.
                    // We consider any pixel with a depth of 0 as a possible candidate for filtering.
                    if (depthArray[depthIndex] == 0)
                    {
                        // From the depth index, we can determine the X and Y coordinates that the index
                        // will appear in the image.  We use this to help us define our filter matrix.
                        int x = depthIndex % width;
                        int y = (depthIndex - x) / width;

                        // The filter collection is used to count the frequency of each
                        // depth value in the filter array.  This is used later to determine
                        // the statistical mode for possible assignment to the candidate.
                        Dictionary<short, short> filterCollection = new Dictionary<short, short>();
                        //short[,] filterCollection = new short[24, 2];

                        // Ampliaremos la matriz de busqueda hasta encontrar suficientes muestras
                        int foundValues = 0;

                        //vamos buscando las mediciones alrededor del punto "negro" hasta encontrar suficientes muestras
                        List<BandCoordinate> surroundingMatrix = new List<BandCoordinate>();
                        bool mustContinue = true;
                        int matrixRadius = 1;
                        int searchIncrement = 0;
                        int maxRadius = width > height ? width : height;
                        do
                        {
                            matrixRadius += 1; //incrementamos el radio de busqueda en esta iteracion
                            searchIncrement++;

                            //Obtenemos las esquinas matriz que rodea al pixel seleccionado con el radio adecuado para esta iteracion
                            BandCoordinate TopLeft = new BandCoordinate() {
                                X = x - matrixRadius < 0 ? 0 : x - matrixRadius,
                                Y = y - matrixRadius < 0 ? 0 : y - matrixRadius
                            };
                            BandCoordinate BottomRight = new BandCoordinate() {
                                X = x + matrixRadius > widthBound ? widthBound : x + matrixRadius,
                                Y = y + matrixRadius > heightBound ? heightBound : y + matrixRadius
                            };

                            //añadimos a la coleccion estadistica los pixeles candidatos que vayamos encontrando en las bandas alrededor del punto
                            //añadimos los bordes superior e inferior
                            for (int xi = TopLeft.X; xi <= BottomRight.X; xi++)
                            {
                                if (foundValues >= foundValuesThreshold) break; //si en algun momento hemos encontrado un valor valido, terminamos
                                foundValues += FindNonZeroDepths(depthArray, width, height, filterCollection, new BandCoordinate() { X = xi, Y = TopLeft.Y });
                                foundValues += FindNonZeroDepths(depthArray, width, height, filterCollection, new BandCoordinate() { X = xi, Y = BottomRight.Y });
                            }

                            //añadimos los bordes izquierdo y derecho evitando los extremos (que ya hemos añadido anteriormente)
                            for (int yi = TopLeft.Y + 1; yi < BottomRight.Y; yi++)
                            {
                                if (foundValues >= foundValuesThreshold) break; //si en algun momento hemos encontrado un valor valido, terminamos
                                foundValues += FindNonZeroDepths(depthArray, width, height, filterCollection, new BandCoordinate() { X = TopLeft.X, Y = yi });
                                foundValues += FindNonZeroDepths(depthArray, width, height, filterCollection, new BandCoordinate() { X = BottomRight.X, Y = yi });
                            }

                            mustContinue = foundValues < foundValuesThreshold && matrixRadius < maxRadius; //hemos encontrado suficientes muestras o hemos excedido los limites de busqueda
                        } while (mustContinue);

                        // Calculamos la moda, que sera nuestro candidato para rellenar este pixel
                        short frequency = 0;
                        short depth = 0;
                        // This loop will determine the statistical mode
                        // of the surrounding pixels for assignment to
                        // the candidate.
                        foreach (short key in filterCollection.Keys)
                        {
                            if (filterCollection[key] > frequency)
                            {
                                frequency = filterCollection[key];
                                depth = key;
                            }
                        }

                        smoothDepthArray[depthIndex] = depth;
                    }
                    else
                    {
                        // If the pixel is not zero, we will keep the original depth.
                        smoothDepthArray[depthIndex] = depthArray[depthIndex];
                    }
                }
            });

            return smoothDepthArray;
        }

        /// <summary>
        /// Actualiza la lista de filterCollection con la profundidad del cuadro determinado por las coordenadas, si es que es un candidato valido
        /// Devuelve el numero de valores encontrados
        /// </summary>
        /// <returns></returns>
        private static int FindNonZeroDepths(short[] depthArray, int width, int height, Dictionary<short, short> filterCollection, BandCoordinate coord)
        {
            int foundValues = 0;

            //ignoramos los que esten fuera de los limites de la imagen
            if (coord.X >= 0 && coord.X <= (width - 1) && coord.Y >= 0 && coord.Y <= (height - 1))
            {
                int index = coord.X + (coord.Y * width);
                // We only want to look for non-0 values
                if (depthArray[index] != 0)
                {
                    short evaluatingDepth = depthArray[index];
                    if (!filterCollection.ContainsKey(evaluatingDepth))
                    {
                        // Cuando no existe esta profundidad, la creamos e inicializamos su frecuencia
                        filterCollection.Add(evaluatingDepth, 0);
                    }
                    //incrementamos la frecuencia esta medicion
                    filterCollection[evaluatingDepth]++;
                    foundValues++;
                }
            }

            return foundValues;
        }
    }
}
