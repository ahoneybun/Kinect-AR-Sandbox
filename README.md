# Augmented Reality Sandbox

Proyecto en el que conservo recursos, adaptaciones y anotaciones que hice sobre el original de la universidad de Ucla.

Proyecto original: [https://arsandbox.ucdavis.edu/](https://arsandbox.ucdavis.edu/)

### Instrucciones

Paso 4:

Instalar Vrui
```shell
wget https://findemor.github.io/Kinect-Augmented-Reality-Sandbox/Linux/Sandbox/Scripts/Build-Ubuntu.sh
bash Build-Ubuntu.sh
```

Instalar Kinect Packages
```shell
cd ~/src
wget https://findemor.github.io/Kinect-Augmented-Reality-Sandbox/Linux/Sandbox/Resources/Kinect-2.8-001.tar.gz
tar xfz Kinect-2.8-001.tar.gz
cd Kinect-2.8-001
make

#### sudo mkdir /usr/local/include/Kinect




sudo make install
make installudevrule
```

https://www.youtube.com/watch?time_continue=1262&v=R0UyMeJ2pYc 27.40

```shell
cd ..
wget https://findemor.github.io/Kinect-Augmented-Reality-Sandbox/Linux/Sandbox/Resources/SARndbox-1.5-001.tar.gz
tar xfz SARndbox-1.5-001.tar.gz





cp ~/src/Kinect-2.8-001/share/Configuration.Kinect ~/src/Vrui-3.1-002/BuildRoot/
cp ~/src/Kinect-2.8-001/BuildRoot/Packages.Kinect ~/src/Vrui-3.1-002/BuildRoot/



cd SARndbox-1.5-001
make

```

conectar la kinect
hacer la calibracion para ver que funciona

```shell
sudo ~/src/Kinect-2.8-001/bin/KinectUtil getCalib 0

cd ~/src/SARndbox-1.5-001
~/src/Kinect-2.8-001/bin/RawKinectViewer -compress 0

```


utilidad para los patrones de calibracion
```shell
~/src/Vrui-3.1-002/bin/XBackground

```



http://idav.ucdavis.edu/~okreylos/ResDev/SARndbox/LinkSoftwareInstallation.html














no se encuentra el fichero... esta en:
findemor@hp-compaq:~/src/SARndbox-1.5-001$ find ../ -name "Configuration.Kinect"
cp ~/src/Kinect-2.8-001/share/Configuration.Kinect ~/src/Vrui-3.1-002/BuildRoot/
cp ~/src/Kinect-2.8-001/BuildRoot/Packages.Kinect ~/src/Vrui-3.1-002/BuildRoot/


/home/findemor/src/Vrui-3.1-002/BuildRoot/Configuration.Kinect: No existe el archivo o el directorio
/home/findemor/src/Vrui-3.1-002/BuildRoot/Packages.Kinect: No existe el archivo o el directorio
