using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AIHW {
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TSPLSPage : Page {
        private int N { get; set; }
        private Random Random { get; set; }
        private List<int> CityOrder { get; set; }
        private double OptimalCost { get; set; }
        private List<(double, double)> Coordinate { get; set; }
        private double[,] Adjacency { get; set; }
        public TSPLSPage() {
            this.InitializeComponent();
            Random = new Random(2223);
            Coordinate = new List<(double, double)>();
        }

        private double TSPCost() {
            double answer = 0d;
            for (int i = 1; i < N; i++) {
                answer += Adjacency[CityOrder[i - 1], CityOrder[i]];
            }
            return answer;
        }

        private (int, int) RandomPair() => (Random.Next(0, N), Random.Next(0, N));

        private double SimulatedAnnealingTSP() {
            OptimalCost *= 1.25;
            (int, int) switchPair;
            double T = 500.0, cost = TSPCost(), deltaCost;
            while (cost > OptimalCost) {
                for (int i = 0; cost > OptimalCost && i < 0x00002FFF; i++) {
                    //SA
                    cost = TSPCost();
                    switchPair = RandomPair();
                    swap(cityOrder[switchPair.first], cityOrder[switchPair.second]);

                    deltaCost = TSPCost() - cost;
                    printf("Cost: %.3f\tT: %.3f\t", cost, T);
                    if (deltaCost >= 0) {
                        printf("P: %.3f\n", exp(-(deltaCost / T)));
                    }
                    else {
                        printf("\n");
                    }
                    if (deltaCost >= 0 && (rand() / (double)0x00007FFF) >= exp(-(deltaCost / T))) {
                        swap(cityOrder[switchPair.first], cityOrder[switchPair.second]);
                    }
                }
                T *= 0.99;
            }
            return cost;
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
                Coordinate[i] = ((Coordinate[i].Item1 - min) * TSPLSCanvas.ActualWidth / delta - 5d,
                    (Coordinate[i].Item2 - min) * TSPLSCanvas.ActualHeight / delta - 5d);
            }
        }

        private void DisplayRoute() {
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

                TSPLSCanvas.Children.Add(current);
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
                TSPLSCanvas.Children.Add(current);
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

        private async void TSPLSCanvas_Drop(object sender, DragEventArgs e) {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0) {
                    TSPLSCanvas.Children.Clear();
                    foreach (var appFile in items.OfType<StorageFile>()) {
                        string rawData = await FileIO.ReadTextAsync(appFile);
                        LoadData(rawData);
                        DisplayPoints();
                    }
                }
            }
        }

        private void TSPLSCanvas_DragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null) {
                e.DragUIOverride.Caption = "Load file";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private void CalculateButtom_Click(object sender, RoutedEventArgs e) {
            CityOrder = new List<int>();
            for (int i = 0; i < N; i++) {
                CityOrder.Add(i);
            }
            DisplayRoute();
        }
    }
}
