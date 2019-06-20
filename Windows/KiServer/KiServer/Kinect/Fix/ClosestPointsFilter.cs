using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiServer.Kinect.Fix
{
    public class ClosestPointsFilter
    {
        int MaxSearchDistance;

        public ClosestPointsFilter(int maxSearchDistance)
        {
            this.MaxSearchDistance = maxSearchDistance;
        }


        public short[] CreateFilteredDepthArray(short[] depthArray, int width, int height)
        {
            short[] smoothDepthArray = new short[depthArray.Length];
            depthArray.CopyTo(smoothDepthArray, 0);
            //short[] smoothDepthArray = depthArray;

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
                    }
                    else
                    {
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

                            FillHole(smoothDepthArray, width, height, y, iLeft, iRight);

                        }
                    }

                }

                //hemos terminado una fila
                //al terminar comprobamos que no estamos en un hueco, por que quedaría "abierto" por la derecha
                if (looking)
                {
                    FillHole(smoothDepthArray, width, height, y, iLeft, iRight, true);
                }
            });


            return smoothDepthArray;
        }



        /// <summary>
        /// Rellena el hueco con valores determinado a partir de un muestreo de las profundidades que lo rodean
        /// </summary>
        private void FillHole(short[] depthArray, int width, int height, int y, int holeLeftXBound, int holeRightXBound, bool isLastHole = false)
        {
            //vamos recorriendo los pixeles del Hueco, para reemplazarlos por un valor valido
            for (int holeX = holeLeftXBound + 1; holeX < (isLastHole ? width : holeRightXBound); holeX++)
            {

                int holeIndex = holeX + y * width; //indice del Hueco en el array plano
                int distance = -1;

                //cogemos la distancia al lado mas corto
                if (holeRightXBound > 0) distance = holeRightXBound - holeX;
                if (holeLeftXBound > 0 && holeX - holeLeftXBound < distance) distance = holeX - holeLeftXBound;

                depthArray[holeIndex] = ChooseDepth(depthArray, width, height, holeX, y, holeLeftXBound, holeRightXBound, distance);
            }
        }

        /// <summary>
        /// Determina cual seria la profundidad que deberia tener un punto basado en lo que le rodea
        /// </summary>
        private short ChooseDepth(short[] depthArray, int width, int height, int x, int y, int holeLeftXBound, int holeRightXBound, int distance)
        {
            short newDepth;

            //TODO MJGS HACER UNA BUSQUEDA 

            if (distance < 0)
            {
                //no tenemos ningun punto pixel valido al alcance, vamos a intentar con el alcance maximo por si en vertical encontramos algo
                newDepth = FastVerticalDepthSearch(depthArray, width, height, x, y);
            }
            else
            {
                //si la distancia al extremo mas cercano es muy grande, reemplazamos usando un mecanismo poco preciso pero rapido
                //poniendo el mismo color que su extremo mas cercano
                if (distance > MaxSearchDistance) //BUSQUEDA RAPIDA
                {
                    //esto se podria mejorar perdiendo eficiencia, por ejemplo mirando si en vertical hay puntos mas cercanos
                    newDepth = FastHorizontalDepthSearch(depthArray, width, height, x, y, holeLeftXBound, holeRightXBound);
                }
                else //BUSQUEDA PRECISA
                {
                    newDepth = PreciseMatrixDepthSearch(depthArray, width, height, x, y, distance);
                }
            }

            return newDepth;
        }

        private static short FastHorizontalDepthSearch(short[] depthArray, int width, int height, int holeX, int holeY, int holeLeftXBound, int holeRightXBound)
        {
            if (holeRightXBound < 0 && holeLeftXBound < 0) return 0; //si no conocemos ningun extremo... no hay nada que hacer

            bool fromRight = true; //determina si el extremo derecho es el mas cercano a este pixel del hueco
            if (holeLeftXBound >= 0) //en otro caso es que no tendremos valor en ese lado, por ejemplo en el margen izquierdo de la imagen
            {
                if (holeRightXBound < holeX || holeX - holeLeftXBound < holeRightXBound - holeX) //si holeRightXBound es menor que el punto actual, significa que no tenemos ese extremo
                {
                    //si la distancia al lado izquierdo es menor que al derecho, actualizamos la distancia y el lado cercano
                    fromRight = false;
                }
            }

            return fromRight ? depthArray[holeRightXBound + holeY * width] : depthArray[holeLeftXBound + holeY * width];
        }


        private static short FastVerticalDepthSearch(short[] depthArray, int width, int height, int holeX, int holeY)
        {
            int maxDistance = height - holeY > holeY ? height - holeY : holeY;
            int x = holeX;

            short depth = 0;

            for (int y = 1; y < maxDistance; y++)
            {
                if (holeY - y >= 0)
                {
                    //busqueda hacia arriba
                    if (depthArray[x + (holeY - y) * width] != 0)
                    {
                        depth = depthArray[x + (holeY - y) * width];
                        break;
                    }
                }
                if (holeY + y < height)
                {
                    //busqueda hacia abajo
                    if (depthArray[x + (holeY + y) * width] != 0)
                    {
                        depth = depthArray[x + (holeY + y) * width];
                        break;
                    }
                }
            }

            return depth;
        }



        private static short PreciseMatrixDepthSearch(short[] depthArray, int width, int height, int x, int y, int distance)
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
                    depth = Convert.ToInt16(key * 10 + 5);
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
                    short evaluatingDepth = Convert.ToInt16(depthArray[index] / 10); //redondeamos a decenas para que se produzca cierta agregacion
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
