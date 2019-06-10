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
        // Will specify how many non-zero pixels within a 1 pixel band
        // around the origin there should be before a filter is applied
        //private int foundValuesThreshold;

        // Will specify how many frames to hold in the Queue for averaging
        private int averageFrameCount;

        // The actual Queue that will hold all of the frames to be averaged
        private Queue<short[]> averageQueue = new Queue<short[]>();
        private short[] correctionLayer = null;

        private bool enableFilter = false;
        private bool enableAverage = false;
        private bool enableHistoricalHolesFilter = false;
        private int FilterMaxSearchDistance = 10;

        private int Width;
        private int Height;

        public DepthFixer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public void SetFilterHistorical(bool enabled)
        {
            enableHistoricalHolesFilter = enabled;
        }

        public void SetFilterHolesFilling(bool enabled, int maxDistance = 10)
        {
            enableFilter = enabled;
            FilterMaxSearchDistance = maxDistance;
        }

        public void SetFilterAverageMoving(bool enabled, int frames = 1)
        {
            enableAverage = enabled;
            averageFrameCount = frames;
        }


        public short[] Fix(short[] depth)
        {
            short[] depthResult = null;
            if (correctionLayer == null) correctionLayer = depth;

            if (this.enableHistoricalHolesFilter) {
                depthResult = ReplaceHolesWithHistorical(depth, correctionLayer);
            }

            if (this.enableFilter)
            {
                depthResult = CreateFilteredDepthArray(depthResult != null ? depthResult : depth, Width, Height);
            }

            if (this.enableAverage)
            {
                depthResult = CreateAverageDepthArray(depthResult != null ? depthResult : depth);
            }

            //si no habia ningun filtro activo
            if (depthResult == null) depthResult = depth;

            //guardamos el ultimo array como capa de correccion
            correctionLayer = depthResult;

            return depthResult;
        }


        private short[] ReplaceHolesWithHistorical(short[] depthArray, short[] correctionLayer)
        {
            // This is a method of Weighted Moving Average per pixel coordinate across several frames of depth data.
            // This means that newer frames are linearly weighted heavier than older frames to reduce motion tails,
            // while still having the effect of reducing noise flickering.

            short[] averagedDepthArray = new short[depthArray.Length];

            //averageQueue.Enqueue(depthArray);
            //CheckForDequeue(1);
            
            for (int index = 0; index < depthArray.Length; index++)
            {
                if (depthArray[index] == 0)
                    averagedDepthArray[index] = correctionLayer[index];
                else
                    averagedDepthArray[index] = depthArray[index];
            }

            // Process each row in parallel
            /*
            Parallel.For(0, 240, depthArrayRowIndex =>
            {
                // Process each pixel in the row
                for (int depthArrayColumnIndex = 0; depthArrayColumnIndex < 320; depthArrayColumnIndex++)
                {
                    var index = depthArrayColumnIndex + (depthArrayRowIndex * 320);
                    if (depthArray[index] == 0)
                        averagedDepthArray[index] = (short)(sumDepthArray[index] / Denominator);
                    else
                        averagedDepthArray[index] = depthArray[index];
                }
            });*/

            return averagedDepthArray;
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
        
        private short[] CreateFilteredDepthArray(short[] depthArray, int width, int height)
        {
            short[] smoothDepthArray = new short[depthArray.Length];

            // Lo utilizaremos para los limites al recorrer los arrays
            int widthBound = width - 1;
            int heightBound = height - 1;

            // Recorrido en vertical (cada fila)
            Parallel.For(0, height, y =>
            {
                int iLeft = -1;
                int iRight = -1;
                bool looking = true;

                // Ahora recorremos horizontalmente buscando "espacios sin datos", a los que llamaremos Huecos
                // cuya profundidad vale 0.
                // Lo que haremos sera encontrar donde empieza y acaba un hueco (en su eje X)
                // y una vez conocidos sus limites, reemplazaremos oportunamente su valor por uno más apropiado

                // Recorrido en horizontal (cada columna)
                for (int x = 0; x <= widthBound; x++)
                {
                    int index = y * width + x; //posicion en el array original
                    if (depthArray[index] == 0)
                    {
                        //si la profundidad era cero, iniciamos la busqueda para determinar cuales son los limites del Hueco
                        looking = true;
                    } else {
                        //si la profundidad no es 0, mantenemos el valor original
                        smoothDepthArray[index] = depthArray[index];

                        //ahora gestionamos los indices que delimitan el Hueco

                        if (!looking)
                        {
                            //si no estamos buscando, es porque el pixel anterior no estaba vacio
                            //asi que desplazamos a la derecha el indice del extremo izquierdo del Hueco
                            iLeft = x;
                        }
                        else
                        {
                            //si estamos buscando, es que el pixel anterior estaba vacio
                            //sin embargo, si hemos llegado aqui es que la profundidad del pixel no es 0, por lo que el hueco ha llegado a su fin.
                            //dejamos por lo tanto de buscar, y corregimos el Hueco
                            iRight = x;
                            looking = false;

                            FillHole(depthArray, width, height, y, smoothDepthArray, iLeft, iRight);

                        }
                    }
                    
                }

                //hemos terminado una fila
                //al terminar comprobamos que no estamos en un hueco, por que quedaría "abierto" por la derecha
                if (looking)
                {
                    FillHole(depthArray, width, height, y, smoothDepthArray, iLeft, iRight, true);
                }
            });


            return smoothDepthArray;
        }

        
        /// <summary>
        /// Rellena el hueco con valores determinado a partir de un muestreo de las profundidades que lo rodean
        /// </summary>
        private void FillHole(short[] depthArray, int width, int height, int y, short[] smoothDepthArray, int holeLeftXBound, int holeRightXBound, bool isLastHole = false)
        {
            //vamos recorriendo los pixeles del Hueco, para reemplazarlos por un valor valido
            for (int holeX = holeLeftXBound + 1; holeX < (isLastHole ? width : holeRightXBound); holeX++)
            {
                int holeIndex = holeX + y * width; //indice del Hueco en el array plano
                int distance = holeRightXBound - holeX; //distancia de este pixel del hueco al extremo derecho
                bool fromRight = true; //determina si el extremo derecho es el mas cercano a este pixel del hueco

                if (holeLeftXBound >= 0) //en otro caso es que no tendremos valor en ese lado, por ejemplo en el margen izquierdo de la imagen
                {
                    if (holeRightXBound < holeX || holeX - holeLeftXBound < distance) //si holeRightXBound es menor que el punto actual, significa que no tenemos ese extremo
                    {
                        //si la distancia al lado izquierdo es menor que al derecho, actualizamos la distancia y el lado cercano
                        fromRight = false;
                        distance = holeX - holeLeftXBound;
                    }
                }

                if (holeRightXBound < 0 && holeLeftXBound < 0)
                {
                    distance = -1;
                }
                
                smoothDepthArray[holeIndex] = ChooseDepth(depthArray, width, height, holeX, y, holeLeftXBound, holeRightXBound, distance, fromRight);
            }
        }

        /// <summary>
        /// Determina cual seria la profundidad que deberia tener un punto basado en lo que le rodea
        /// </summary>
        private short ChooseDepth(short[] depthArray, int width, int height, int x, int y, int holeLeftXBound, int holeRightXBound, int distance, bool fromRight)
        {
            short newDepth;

            //TODO MJGS HACER UNA BUSQUEDA 

            if (distance < 0)
            {
                //no tenemos ningun punto pixel valido al alcance, vamos a intentar con el alcance maximo por si en vertical encontramos algo
                newDepth = PreciseDepthSearch(depthArray, width, height, x, y, FilterMaxSearchDistance);
            }
            else
            {
                //si la distancia al extremo mas cercano es muy grande, reemplazamos usando un mecanismo poco preciso pero rapido
                //poniendo el mismo color que su extremo mas cercano
                if (distance > FilterMaxSearchDistance) //BUSQUEDA RAPIDA
                {
                    //esto se podria mejorar perdiendo eficiencia, por ejemplo mirando si en vertical hay puntos mas cercanos
                    newDepth = fromRight ? depthArray[holeRightXBound + y * width] : depthArray[holeLeftXBound + y * width];
                }
                else //BUSQUEDA PRECISA
                {
                    newDepth = PreciseDepthSearch(depthArray, width, height, x, y, distance);
                }
            }

            return newDepth;
        }

        private static short PreciseDepthSearch(short[] depthArray, int width, int height, int x, int y, int distance)
        {
            //establecemos el tamaño de la matriz en base a la distancia
            int yFrom = y - distance >= 0 ? y - distance : 0;
            int yTo = y + distance <= height - 1 ? y + distance : height - 1;
            int xFrom = x - distance >= 0 ? x - distance : 0;
            int xTo = x + distance <= width - 1 ? x + distance : width - 1;

            //buscamos en la matriz todos las profundidades distintas de 0
            //y construimos una coleccion que recoge cuantas veces aparece cada profundidad
            Dictionary<short, short> filterCollection = new Dictionary<short, short>();
            int found = 0;
            for (int yi = yFrom; yi <= yTo; yi++)
            {
                for (int xi = xFrom; xi <= xTo; xi++)
                {
                    //TODO estaria bien "agregar" quitando un nivel de precision a la profundidad, porque si no la modo
                    //siempre tendra frecuencia 1
                    found += FindNonZeroDepths(depthArray, width, height, filterCollection, xi, yi);
                }
            }

            //Cogemos la profundidad con mas ocurrencias (la moda) sustituimos el pixel del hueco por ese valor
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

            return depth;
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
