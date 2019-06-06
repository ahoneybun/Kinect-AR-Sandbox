

let smoothDepthArray = 
[
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0
];



let depthArray = 
[
  1, 1, 2, 3, 4, 5, 6, 7, 8, 9,
 10, 0,12,13,14,15,16,17,18,19,
 20,21,22,23,24,25,26,27,28,29,
 30,31,32,33,34,35,36,37,38,39,
 40,41,42,43,44,45,46,47,48,49,
 50,51,52,53,54,55,56,57,58,59,
 60,61,62,63,64,65,66,67,68,69,
 70,71,72,73,74,75,76,77,78,79,
 80,81,82,83,84,85,86,87,88,89
];


let width = 10;
let height = 9;
let widthBound = width - 1;

let lArray = {};
let rArray = {};
let sArray = {}; //huecos
let iArray = {}; //el indice por el que vamos para cada fila

let nextRows = [0, 1, 2, 3, 4, 5, 6, 7, 8];

//inicializacion
for(let h = 0; h < height; h++) {
  lArray[h] = 0;
  rArray[h] = widthBound;
  iArray[h] = 0;
  sArray[h] = true; //todos los huecos se estan buscando
}

let nextRowPosition = 0;
let index = 0;
let processedIndexes = 0;
while (processedIndexes < depthArray.length) {

  let row = Math.floor(index / width);
  let col = index % width;

  //vamos recorriendo el array buscando huecos
  if (depthArray[index] != 0)
  {
      //mantenemos el valor original
      smoothDepthArray[index] = depthArray[index];
      
      //si no estamos buscando, es porque el pixel anterior no estaba vacio, tenemos un valor por la izquierda
      if (!sArray[row]) {
        lArray[row] = index;
      }
      else
      {
          //si estamos buscando, es que el pixel anterior estaba vacio, buscamos el valor por la derecha
          rArray[row] = col;
          sArray[row] = false; //ya hemos terminado esta busqueda local

          //como tenemos valor por ambos extremos, establecemos a ese valor todos los pixeles intermedios
          //aqui seria mejor hacerlo tambien en altura y coger la moda del cuadrado

          //vemos cual es el mas cercano de los dos lados
          //TODO: EN REALIDAD, HABRIA QUE BUSCAR CUANDO HEMOS ACABADO DE ALINEAR VERTICALMENTE
          let replacingDepthIndex = lArray[row];
          if (rArray[row] - index < index - lArray[row]) replacingDepthIndex = rArray[row];
          for (let j = lArray[row]; j <= rArray[row]; j++)
          {
              smoothDepthArray[j + row * width] = depthArray[replacingDepthIndex + row * width];
          }
      }
  }
  else
  {
      //si la profundidad era cero, iniciamos la busqueda
      sArray[row] = true;
  }

  console.log(draw(smoothDepthArray));


  iArray[row]++; //preparamos el siguiente indice que se procesara
  processedIndexes++;
  //determinamos que posicion sera la siguiente en procesarse
  if (!sArray[row]) //si no estamos buscando es porque hemos encontrado el fin del hueco en esta fila, podemos saltar al la siguiente con el hueco sin cerrar
  {
    if (nextRows.length == 0) break; //hemos terminado, no hay mas filas

    //si la columna es widthBound, quitamos esta fila, se ha acabado su procesamiento
    if (col == widthBound) nextRows.splice(nextRowPosition, 1);

    //vemos por que indice vamos de la siguiente fila
    nextRowPosition = (nextRowPosition + 1) % nextRows.length;
    let nextRow = nextRows[nextRowPosition];
    index = iArray[nextRow] + nextRow * width;
  } else {
    index = iArray[row] + row * width;
  }
} 





function draw(a) {
  let index = 0;
  let ret = "===============================";
  
  for(let i = 0; i < height; i++) {
    ret = ret + "\n";
    for(let j = 0; j < width; j++) {
      ret = ret + (a[index] < 10 ? " " + a[index] : a[index]) + ",";
      index++;
    }
  }
  ret = ret + "\n===============================";

  return ret;
}


console.log(draw(smoothDepthArray));



















/*
[
   0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
  10,11,12,13,14,15,16,17,18,19,
  20,21,22,23,24,25,26,27,28,29,
  30,31,32,33,34,35,36,37,38,39,
  40,41,42,43,44,45,46,47,48,49,
  50,51,52,53,54,55,56,57,58,59,
  60,61,62,63,64,65,66,67,68,69,
  70,71,72,73,74,75,76,77,78,79,
  80,81,82,83,84,85,86,87,88,89
]
*/