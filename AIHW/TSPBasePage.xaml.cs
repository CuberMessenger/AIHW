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

namespace AIHW {
    public partial class TSPBasePage : Page {
        internal int N { get; set; }
        internal double Cost { get; set; }
        internal Random Random { get; set; }
        internal List<Line> Lines { get; set; }
        internal double OptimalCost { get; set; }
        internal double[,] Adjacency { get; set; }
        internal List<int> CityOrder { get; set; }
        internal List<(double, double)> Coordinate { get; set; }

        public event EventHandler DataLoadedEvent;

        public TSPBasePage() {
            this.InitializeComponent();
            this.Random = new Random((int)DateTime.Now.Ticks);
            Coordinate = new List<(double, double)>();
            Lines = new List<Line>();
        }

        internal double TSPCost() {
            double answer = 0d;
            for (int i = 1; i < N; i++) {
                answer += Adjacency[CityOrder[i - 1], CityOrder[i]];
            }
            return answer;
        }

        internal void NormalizeCoordinateToCanvas(Canvas canvas) {
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < N; i++) {
                min = Coordinate[i].Item1 < min ? Coordinate[i].Item1 : min;
                min = Coordinate[i].Item2 < min ? Coordinate[i].Item2 : min;
                max = Coordinate[i].Item1 > max ? Coordinate[i].Item1 : max;
                max = Coordinate[i].Item2 > max ? Coordinate[i].Item2 : max;
            }
            var delta = max - min;
            for (int i = 0; i < N; i++) {
                Coordinate[i] = ((Coordinate[i].Item1 - min) * canvas.ActualWidth / delta - 5d,
                    (Coordinate[i].Item2 - min) * canvas.ActualHeight / delta - 5d);
            }
        }

        internal void DisplayRoute(Canvas canvas) {
            foreach (Line line in Lines) {
                canvas.Children.Remove(line);
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
                canvas.Children.Add(current);
            }
        }

        internal void DisplayPoints(Canvas canvas) {
            NormalizeCoordinateToCanvas(canvas);
            SolidColorBrush darkBlueBrush = new SolidColorBrush(Windows.UI.Colors.DarkBlue);
            for (int i = 0; i < N; i++) {
                Ellipse current = new Ellipse {
                    Fill = darkBlueBrush,
                    Width = 10d,
                    Height = 10d
                };
                canvas.Children.Add(current);
                current.SetValue(Canvas.LeftProperty, Coordinate[i].Item1);
                current.SetValue(Canvas.TopProperty, Coordinate[i].Item2);
            }
        }


        internal double L2Norm((double, double) a, (double, double) b)
            => Math.Sqrt(Math.Pow(a.Item1 - b.Item1, 2d) + Math.Pow(a.Item2 - b.Item2, 2d));

        internal void LoadData(string rawData) {
            var allraw = rawData.Split("\r\n".ToArray());
            var info = allraw[6].Split(" ");
            N = int.Parse(info[0]);
            OptimalCost = double.Parse(info[1]) * 1.05d;
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

        internal async void TSPLSSACanvas_Drop(object sender, DragEventArgs e) {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0) {
                    var canvas = sender as Canvas;
                    canvas.Children.Clear();
                    foreach (var appFile in items.OfType<StorageFile>()) {
                        string rawData = await FileIO.ReadTextAsync(appFile);
                        LoadData(rawData);
                        DisplayPoints(canvas);
                        CityOrder = new List<int>();
                        for (int i = 0; i < N; i++) {
                            CityOrder.Add(i);
                        }
                        CityOrder = CityOrder.OrderBy(a => Random.Next(0, 3) - 1).ToList();
                        Cost = TSPCost();
                        DisplayRoute(canvas);
                        DataLoadedEvent?.Invoke(this, null);
                    }
                }
            }
        }

        internal void TSPLSSACanvas_DragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null) {
                e.DragUIOverride.Caption = "Load file";
                e.DragUIOverride.IsContentVisible = true;
            }
        }
    }
}
