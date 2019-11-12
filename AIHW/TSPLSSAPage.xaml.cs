using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace AIHW {
    public sealed partial class TSPLSSAPage : Page {
        private int N { get; set; }
        private double Cost { get; set; }
        private Random Random { get; set; }
        private List<Line> Lines { get; set; }
        private double Temperature { get; set; }
        private double OptimalCost { get; set; }
        private double[,] Adjacency { get; set; }
        private List<int> CityOrder { get; set; }
        private bool DisplayEveryStep { get; set; }
        private List<(double, double)> Coordinate { get; set; }
        public TSPLSSAPage() {
            this.InitializeComponent();
            Random = new Random(2223);
            Coordinate = new List<(double, double)>();
            Lines = new List<Line>();
            DisplayEveryStep = true;
        }

        private double TSPCost() {
            double answer = 0d;
            for (int i = 1; i < N; i++) {
                answer += Adjacency[CityOrder[i - 1], CityOrder[i]];
            }
            return answer;
        }

        private (int, int) RandomPair() => (Random.Next(0, N), Random.Next(0, N));

        private void SwapPair((int, int) switchPair) {
            int temp = CityOrder[switchPair.Item1];
            CityOrder[switchPair.Item1] = CityOrder[switchPair.Item2];
            CityOrder[switchPair.Item2] = temp;
        }

        private async void SimulatedAnnealingTSP() {
            OptimalCost *= 1.2;
            (int, int) switchPair;
            Cost = TSPCost();
            Temperature = 500d;
            double deltaCost;
            while (Cost > OptimalCost) {
                for (int i = 0; Cost > OptimalCost && i < 0x00002FFF; i++) {
                    //SA
                    Cost = TSPCost();
                    switchPair = RandomPair();
                    SwapPair(switchPair);

                    deltaCost = TSPCost() - Cost;
                    Console.WriteLine("Cost: %.3f\tT: %.3f\t", Cost, Temperature);
                    if (deltaCost >= 0) {
                        Console.WriteLine("P: %.3f\n", Math.Exp(-(deltaCost / Temperature)));
                    }
                    else {
                        Console.WriteLine("\n");
                    }
                    if (deltaCost >= 0 && (Random.NextDouble() >= Math.Exp(-(deltaCost / Temperature)))) {
                        SwapPair(switchPair);
                    }
                    if (DisplayEveryStep) {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DisplayRoute());
                    }
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => DisplayRoute());
                Temperature *= 0.99;
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute();
                TSPLSResultTextBlock.Text += "Done!";
            });
        }

        private void NormalizeCoordinateToCanvas() {
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < N; i++) {
                min = Coordinate[i].Item1 < min ? Coordinate[i].Item1 : min;
                min = Coordinate[i].Item2 < min ? Coordinate[i].Item2 : min;
                max = Coordinate[i].Item1 > max ? Coordinate[i].Item1 : max;
                max = Coordinate[i].Item2 > max ? Coordinate[i].Item2 : max;
            }
            var delta = max - min;
            for (int i = 0; i < N; i++) {
                Coordinate[i] = ((Coordinate[i].Item1 - min) * TSPLSSACanvas.ActualWidth / delta - 5d,
                    (Coordinate[i].Item2 - min) * TSPLSSACanvas.ActualHeight / delta - 5d);
            }
        }

        public void DisplayRoute() {
            Bindings.Update();
            foreach (Line line in Lines) {
                TSPLSSACanvas.Children.Remove(line);
            }
            SolidColorBrush skyBlueBrush = new SolidColorBrush(Windows.UI.Colors.SkyBlue);
            for (int i = 1; i < N; i++) {
                Line current = new Line {
                    Stroke = skyBlueBrush,
                    StrokeThickness = 1.5d,
                    X1 = Coordinate[CityOrder[i - 1]].Item1 + 5d,
                    Y1 = Coordinate[CityOrder[i - 1]].Item2 + 5d,
                    X2 = Coordinate[CityOrder[i]].Item1 + 5d,
                    Y2 = Coordinate[CityOrder[i]].Item2 + 5d
                };
                Lines.Add(current);
                TSPLSSACanvas.Children.Add(current);
            }
        }

        private void DisplayPoints() {
            NormalizeCoordinateToCanvas();
            SolidColorBrush darkBlueBrush = new SolidColorBrush(Windows.UI.Colors.DarkBlue);
            for (int i = 0; i < N; i++) {
                Ellipse current = new Ellipse {
                    Fill = darkBlueBrush,
                    Width = 10d,
                    Height = 10d
                };
                TSPLSSACanvas.Children.Add(current);
                current.SetValue(Canvas.LeftProperty, Coordinate[i].Item1);
                current.SetValue(Canvas.TopProperty, Coordinate[i].Item2);
            }
        }


        private double L2Norm((double, double) a, (double, double) b)
            => Math.Sqrt(Math.Pow(a.Item1 - b.Item1, 2d) + Math.Pow(a.Item2 - b.Item2, 2d));

        private void LoadData(string rawData) {
            var allraw = rawData.Split("\r\n".ToArray());
            var info = allraw[6].Split(" ");
            N = int.Parse(info[0]);
            OptimalCost = double.Parse(info[1]);
            Adjacency = new double[N, N];

            for (int i = 0; i < N; i++) {
                var raw = allraw[i + 7].Split(" ");
                Coordinate.Add((double.Parse(raw[1]), double.Parse(raw[2])));
            }
            for (int i = 0; i < N; i++) {
                for (int j = i; j < N; j++) {
                    Adjacency[i, j] = L2Norm(Coordinate[i], Coordinate[j]);
                    Adjacency[j, i] = Adjacency[i, j];
                }
            }
        }

        private async void TSPLSSACanvas_Drop(object sender, DragEventArgs e) {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0) {
                    TSPLSSACanvas.Children.Clear();
                    foreach (var appFile in items.OfType<StorageFile>()) {
                        string rawData = await FileIO.ReadTextAsync(appFile);
                        LoadData(rawData);
                        DisplayPoints();
                        CityOrder = new List<int>();
                        for (int i = 0; i < N; i++) {
                            CityOrder.Add(i);
                        }
                        Cost = TSPCost();
                        DisplayRoute();
                    }
                }
            }
        }

        private void TSPLSSACanvas_DragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null) {
                e.DragUIOverride.Caption = "Load file";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private void CalculateButtom_Click(object sender, RoutedEventArgs e) => Task.Run(() => SimulatedAnnealingTSP());
    }
}
