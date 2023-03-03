using CarGallery.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

namespace CarGallery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Car> Cars { get; set; }
        private CancellationTokenSource cToken;
        private Stopwatch stopwatch;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Cars = new();
            CarsList.ItemsSource = Cars;
            stopwatch = new();
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            cToken = new();
            if (rb_single.IsChecked == true)
                SingleCars(cToken.Token);
            else
                MultiCars(cToken.Token);
            CancelBtn.IsEnabled = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            cToken.Cancel();
        }
        private void SingleCars(CancellationToken token)
        {
            Cars.Clear();
            new Thread(() =>
            {
                stopwatch = Stopwatch.StartNew();
                var directory = new DirectoryInfo(@"..\..\..\FakeDatas");
                foreach (var file in directory.GetFiles())
                {

                    if (file.Extension == ".json")
                    {
                        var text = File.ReadAllText(file.FullName);

                        var carlist = JsonSerializer.Deserialize<List<Car>>(text);

                        if (carlist is not null)
                            foreach (var car in carlist)
                            {
                                if (token.IsCancellationRequested) break;

                                Dispatcher.Invoke(() => Cars?.Add(car));
                                Dispatcher.Invoke(() => { StartBtn.IsEnabled = false; });
                                Dispatcher.Invoke(() => { Timertxtbox.Text = stopwatch.Elapsed.ToString("mm\\:ss\\.ff"); });
                                Thread.Sleep(200);
                            }
                    }
                }
                Dispatcher.Invoke(() => { Timertxtbox.Text = stopwatch.Elapsed.ToString("mm\\:ss\\.ff"); });
            }).Start();
        }

        private void MultiCars(CancellationToken token)
        {
            Cars.Clear();
            var sync = new object();
            var dirInfo = new DirectoryInfo(@"..\..\..\FakeDatas");
            stopwatch = Stopwatch.StartNew();
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension == ".json")
                {
                    ThreadPool.QueueUserWorkItem(s =>
                    {
                        var text = File.ReadAllText(file.FullName);
                        var carsList = JsonSerializer.Deserialize<List<Car>>(text);
                        if (carsList is not null)
                        {
                            foreach (var car in carsList)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    Dispatcher.Invoke(() => { Timertxtbox.Text = stopwatch.Elapsed.ToString("mm\\:ss\\.ff"); });
                                    break;
                                }
                                lock (sync)
                                    Dispatcher.Invoke(() => Cars?.Add(car));


                                Dispatcher.Invoke(() => { Timertxtbox.Text = stopwatch.Elapsed.ToString("mm\\:ss\\.ff"); });
                                Dispatcher.Invoke(() => { StartBtn.IsEnabled = false; });
                                Thread.Sleep(100);
                            }
                        }
                    });
                }

            }
        }
    }
}
