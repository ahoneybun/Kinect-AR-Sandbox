using KiServer.Kinect;
using KiServer.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiServer
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int PORT_NO = 5000;
        string SERVER_IP = "127.0.0.1";
        bool started = false;
        BackgroundTask backgroundTask = null;

        public MainWindow()
        {
            InitializeComponent();

            backgroundTask = new BackgroundTask();

            backgroundTask.SetFixedCanvas(canvasFixed);
            backgroundTask.SetRawCanvas(canvasRaw);
            backgroundTask.SetFpsText(fpsText);
            backgroundTask.SetRawColorCanvas(canvasRawColor);

            backgroundTask.EnablePreview = (bool)previewCheck.IsChecked;

            Thread thread = new Thread(new ThreadStart(backgroundTask.Start));
            thread.Start();
            CheckFiltersStatus();
        }

        private void TPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                PORT_NO = Convert.ToInt32(tPort.Text);
            }
            catch
            {
                tPort.Text = PORT_NO.ToString();
            }
        }

        private void BStart_Click(object sender, RoutedEventArgs e)
        {
            started = !started;
            bStart.Content = started ? "Stop" : "Start";
            tPort.IsEnabled = !started;

            if (started)
            {
                //Arrancamos el servidor TCP
                backgroundTask.StartTCP(PORT_NO, SERVER_IP);

            }
            else
            {
                //detenemos el servidor TCP
                backgroundTask.StopTCP();
            }
        }

        private void Check_Changed(object sender, RoutedEventArgs e)
        {
            CheckFiltersStatus();
        }

        private void CheckFiltersStatus()
        {
            if (backgroundTask != null)
            {
                //avg filter
                avgSlider.IsEnabled = (bool)avgCheck.IsChecked;
                int avgValue = Convert.ToInt32(avgSlider.Value);
                backgroundTask.SetFilterAverageMoving((bool)avgCheck.IsChecked, avgValue);
                avgText.Content = avgValue + " frames";


                //mode filter
                backgroundTask.SetFilterModeMoving((bool)modeCheck.IsChecked, avgValue);

                //historical
                backgroundTask.SetFilterHistorical((bool)histCheck.IsChecked);

                //stat holes filling
                statSlider.IsEnabled = (bool)statCheck.IsChecked;
                int statValue = Convert.ToInt32(statSlider.Value);
                backgroundTask.SetFilterHolesFilling((bool)statCheck.IsChecked, statValue);
                statText.Content = statValue + " pixels";
            }
        }

        private void Preview_Checked(object sender, RoutedEventArgs e)
        {
            if (backgroundTask != null) backgroundTask.EnablePreview = (bool)previewCheck.IsChecked;
        }
    }
}
