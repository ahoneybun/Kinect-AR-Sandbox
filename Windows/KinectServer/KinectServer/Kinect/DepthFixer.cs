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
        //private int foundValuesThreshold;

        // Will specify how many frames to hold in the Queue for averaging
        private int averageFrameCount;

        // The actual Queue that will hold all of the frames to be averaged
        private Queue<short[]> averageQueue = new Queue<short[]>();

        private bool enableFilter = false;
        private bool enableAverage = false;

        public DepthFixer(bool enableFilter, bool enableAverage, int averageFrameCount)
        {
            //this.foundValuesThreshold = foundValuesThreshold;
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
        }
        */
        private short[] CreateFilteredDepthArray(short[] depthArray, int width, int height)
        {
            /////////////////////////////////////////////////////////////////////////////////////
            // based on this Codeplex Project https://www.codeproject.com/Articles/317974/KinectDepthSmoothing
            /////////////////////////////////////////////////////////////////////////////////////

            short[] smoothDepthArray = new short[depthArray.Length];

            // We will be using these numbers for constraints on indexes
            int widthBound = width - 1;
            int heightBound = height - 1;
            int MAX_MODE_SEARCH_DISTANCE = 10;
            // We process each row



            //TODO hay que hacer la busqueda por bloques, porque lo de la izquierda se me esta saliendo por la derecha (hay que paralelizar las filas)

            //TODO deberia buscarse tambien en altura, y quiza incluso encontrar la moda por matrices alrededor
            Parallel.For(0, height, y =>
            //for (int index = 0; index < depthArray.Length; index++)
            {

                int iLeft = 0;
                int iRight = widthBound;
                bool looking = true;

                for (int x = 0; x <= widthBound; x++)
                {
                    int index = y * width + x;

                    if (depthArray[index] != 0)
                    {
                        //mantenemos el valor original
                        smoothDepthArray[index] = depthArray[index];

                        //si no estamos buscando, es porque el pixel anterior no estaba vacio, tenemos un valor por la izquierda
                        if (!looking)
                        {
                            iLeft = x;
                        }
                        else
                        {
                            //si estamos buscando, es que el pixel anterior estaba vacio, buscamos el valor por la derecha
                            iRight = x;
                            looking = false; //ya hemos terminado esta busqueda local

                            //como tenemos valor por ambos extremos, establecemos a ese valor todos los pixeles intermedios
                            //aqui seria mejor hacerlo tambien en altura y coger la moda del cuadrado

                            //vemos cual es el mas cercano de los dos lados
                            /*int replacingDepthIndex = iLeft;
                            if (iRight - x < x - iLeft) replacingDepthIndex = iRight;
                            for (int j = iLeft + 1; j < iRight; j++)
                            {
                                smoothDepthArray[j + y * width] = depthArray[replacingDepthIndex];
                            }*/


                            //sustituimos cada hueco encontrado
                            for (int slotX = iLeft + 1; slotX < iRight; slotX++)
                            {
                                int jIndex = slotX + y * width;
                                int distance = iRight - slotX;
                                bool fromRight = true;
                                if (slotX - iLeft < distance)
                                {
                                    fromRight = false;
                                    distance = slotX - iLeft;
                                }

                                if (distance > MAX_MODE_SEARCH_DISTANCE) //BUSQUEDA RAPIDA
                                {
                                    //buscar en vertical?
                                    smoothDepthArray[jIndex] = fromRight ? depthArray[iRight + y * width] : depthArray[iLeft + y * width];
                                }
                                else //BUSQUEDA PRECISA
                                {
                                    //establecemos el tamaño de la matriz en base a la distancia
                                    int yFrom = y - distance >= 0 ? y - distance : 0;
                                    int yTo = y + distance <= heightBound ? y + distance : heightBound;
                                    int xFrom = slotX - distance >= 0 ? slotX - distance : 0;
                                    int xTo = slotX + distance <= widthBound ? slotX + distance : widthBound;

                                    //buscamos en la matriz que lo rodea con la distancia minima necesaria
                                    Dictionary<short, short> filterCollection = new Dictionary<short, short>();
                                    int found = 0;
                                    for (int yi = yFrom; yi <= yTo; yi++)
                                    {
                                        for (int xi = xFrom; xi <= xTo; xi++)
                                        {
                                            found += FindNonZeroDepths(depthArray, width, height, filterCollection, xi, yi);
                                        }
                                    }



                                    //asignamos la que tenga mayor moda
                                    short mode = 0;
                                    short depth = 0;
                                    foreach (short key in filterCollection.Keys)
                                    {
                                        if (filterCollection[key] > mode)
                                        {
                                            depth = key;
                                            mode = filterCollection[key];
                                        };
                                    }
                                    smoothDepthArray[jIndex] = depth;
                                }
                            }



                            

                        }
                    }
                    else
                    {
                        //si la profundidad era cero, iniciamos la busqueda
                        looking = true;
                    }
                }

                //hemos terminado una fila

                //al terminar comprobamos que no se ha quedado abierto por la derecha
                if (looking)
                {
                    for (int j = iLeft + 1; j < width; j++)
                    {
                        depthArray[j] = depthArray[iLeft];
                    }
                }
            });



            //Parallel.For(0, height, depthArrayRowIndex =>
            //for (int depthArrayRowIndex = 0; depthArrayRowIndex < height; depthArrayRowIndex++)


            return smoothDepthArray;
        }

        /// <summary>
        /// Actualiza la lista de filterCollection con la profundidad del cuadro determinado por las coordenadas, si es que es un candidato valido
        /// Devuelve el numero de valores encontrados
        /// </summary>
        /// <returns></returns>
        private static int FindNonZeroDepths(short[] depthArray, int width, int height, Dictionary<short, short> filterCollection, int x, int y)
        {
            int foundValues = 0;

            //ignoramos los que esten fuera de los limites de la imagen
            if (x >= 0 && x <= (width - 1) && y >= 0 && y <= (height - 1))
            {
                int index = x + (y * width);
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
