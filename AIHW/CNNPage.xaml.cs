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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace AIHW {
    public sealed partial class CNNPage : Page {
        public CNNPage() {
            this.InitializeComponent();
        }



        private void ClearInkCanvasButtonClick(object sender, RoutedEventArgs e) {

        }

        private void PredictFromInkButtonClick(object sender, RoutedEventArgs e) {

        }

        private void TestButtonClick(object sender, RoutedEventArgs e) {

        }

        private void NextButtonClick(object sender, RoutedEventArgs e) {

        }

        private void TrainButtonClick(object sender, RoutedEventArgs e) {

        }

        private async void GridDropAsync(object sender, DragEventArgs e) {
            if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0) {
                    foreach (var appFile in items.OfType<StorageFile>()) {

                    }
                }
                TestTextBlock.Text = "Load Data Done";
                TrainButton.IsEnabled = true;
            }
        }

        private void GridDragOver(object sender, DragEventArgs e) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            if (e.DragUIOverride != null) {
                e.DragUIOverride.Caption = "Load file";
                e.DragUIOverride.IsContentVisible = true;
            }
        }
    }
}
