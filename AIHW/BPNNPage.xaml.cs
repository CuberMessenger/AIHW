using System;
using System.Buffers.Binary;
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
using Windows.UI.Xaml.Shapes;
using AIHW.BPNN;
using System.Threading;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AIHW {
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class BPNNPage : Page {
        private float[,,] TrainData { get; set; }
        private int[] TrainLabel { get; set; }
        private float[,,] TestData { get; set; }
        private int[] TestLabel { get; set; }
        private Random Random { get; set; }
        private Rectangle[,] Rectangles { get; set; }
        private BackPropagationNeuralNetwork BackPropagationNeuralNetwork { get; set; }

        private static SolidColorBrush BlackBrush = new SolidColorBrush(Windows.UI.Colors.Black);
        private static SolidColorBrush WhiteBrush = new SolidColorBrush(Windows.UI.Colors.White);

        public BPNNPage() {
            this.InitializeComponent();
            Random = new Random();
            Rectangles = new Rectangle[28, 28];
            for (int r = 0; r < 28; r++) {
                for (int c = 0; c < 28; c++) {
                    Rectangles[r, c] = new Rectangle();
                    ImageGrid.Children.Add(Rectangles[r, c]);
                    Rectangles[r, c].SetValue(Grid.RowProperty, r);
                    Rectangles[r, c].SetValue(Grid.ColumnProperty, c);
                }
            }

            var networkShape = new int[] { 28 * 28, 32, 10 };
            var epoch = 50;
            var learnRate = 0.001f;

            BackPropagationNeuralNetwork = new BackPropagationNeuralNetwork(networkShape, epoch, learnRate);
        }

        private void ParseData(Stream trainDataStream, Stream trainLabelStream, Stream testDataStream, Stream testLabelStream) {
            if (trainDataStream != null) {
                using (BinaryReader reader = new BinaryReader(trainDataStream)) {
                    byte[] buffer = new byte[4];
                    trainDataStream.Read(buffer, 0, 4);
                    trainDataStream.Read(buffer, 0, 4);
                    int numOfImage = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    trainDataStream.Read(buffer, 0, 4);
                    int numOfRow = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    trainDataStream.Read(buffer, 0, 4);
                    int numOfColumn = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    TrainData = new float[numOfImage, numOfRow, numOfColumn];
                    for (int i = 0; i < numOfImage; i++) {
                        for (int r = 0; r < numOfRow; r++) {
                            for (int c = 0; c < numOfColumn; c++) {
                                TrainData[i, r, c] = reader.ReadByte() / 255f;
                            }
                        }
                    }
                }
            }

            if (trainLabelStream != null) {
                using (BinaryReader reader = new BinaryReader(trainLabelStream)) {
                    byte[] buffer = new byte[4];
                    trainLabelStream.Read(buffer, 0, 4);
                    trainLabelStream.Read(buffer, 0, 4);
                    int numOfImage = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    TrainLabel = new int[numOfImage];
                    for (int i = 0; i < numOfImage; i++) {
                        TrainLabel[i] = reader.ReadByte();
                    }
                }
            }

            if (testDataStream != null) {
                using (BinaryReader reader = new BinaryReader(testDataStream)) {
                    byte[] buffer = new byte[4];
                    testDataStream.Read(buffer, 0, 4);
                    testDataStream.Read(buffer, 0, 4);
                    int numOfImage = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    testDataStream.Read(buffer, 0, 4);
                    int numOfRow = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    testDataStream.Read(buffer, 0, 4);
                    int numOfColumn = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    TestData = new float[numOfImage, numOfRow, numOfColumn];
                    for (int i = 0; i < numOfImage; i++) {
                        for (int r = 0; r < numOfRow; r++) {
                            for (int c = 0; c < numOfColumn; c++) {
                                TestData[i, r, c] = reader.ReadByte() / 255f;
                            }
                        }
                    }
                }
            }

            if (testLabelStream != null) {
                using (BinaryReader reader = new BinaryReader(testLabelStream)) {
                    byte[] buffer = new byte[4];
                    testLabelStream.Read(buffer, 0, 4);
                    testLabelStream.Read(buffer, 0, 4);
                    int numOfImage = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    TestLabel = new int[numOfImage];
                    for (int i = 0; i < numOfImage; i++) {
                        TestLabel[i] = reader.ReadByte();
                    }
                }
            }
        }

        private async void GridDropAsync(object sender, DragEventArgs e) {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0) {
                    foreach (var appFile in items.OfType<StorageFile>()) {
                        if (appFile.Name == "t10k-images.idx3-ubyte") {
                            ParseData(null, null, (await appFile.OpenReadAsync()).AsStreamForRead(), null);
                        }
                        if (appFile.Name == "t10k-labels.idx1-ubyte") {
                            ParseData(null, null, null, (await appFile.OpenReadAsync()).AsStreamForRead());
                        }
                        if (appFile.Name == "train-images.idx3-ubyte") {
                            ParseData((await appFile.OpenReadAsync()).AsStreamForRead(), null, null, null);
                        }
                        if (appFile.Name == "train-labels.idx1-ubyte") {
                            ParseData(null, (await appFile.OpenReadAsync()).AsStreamForRead(), null, null);
                        }
                    }
                }
                TestTextBlock.Text = "Load Done";
            }
        }

        private void GridDragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null) {
                e.DragUIOverride.Caption = "Load file";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private void NextButtonClick(object sender, RoutedEventArgs e) {
            int i = Random.Next(0, 60000);
            for (int r = 0; r < 28; r++) {
                for (int c = 0; c < 28; c++) {
                    Rectangles[r, c].Fill = TrainData[i, r, c] == 0 ? BlackBrush : WhiteBrush;
                }
            }
        }

        private async void TrainButtonClick(object sender, RoutedEventArgs e) {
            BackPropagationNeuralNetwork.Train(TrainData, TrainLabel, Dispatcher, TestTextBlock);

            //TestTextBlock.Text = "Train Done!";
        }

        private void TestButtonClick(object sender, RoutedEventArgs e) {
            var accuracy = BackPropagationNeuralNetwork.Test(TestData, TestLabel);
            TestTextBlock.Text = $"Test Done!\nAccuracy = {accuracy}";
        }
    }
}
