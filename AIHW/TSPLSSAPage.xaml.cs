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
    public sealed partial class TSPLSSAPage : TSPBasePage {
        private double Temperature { get; set; }
        private bool DisplayEveryStep { get; set; }
        public TSPLSSAPage() : base() {
            this.InitializeComponent();
            DisplayEveryStep = true;
        }

        private (int, int) RandomPair() => (Random.Next(0, N), Random.Next(0, N));

        private void SwapPair((int, int) switchPair) {
            int temp = CityOrder[switchPair.Item1];
            CityOrder[switchPair.Item1] = CityOrder[switchPair.Item2];
            CityOrder[switchPair.Item2] = temp;
        }

        private async void SimulatedAnnealingTSP() {
            OptimalCost *= 1.1;
            (int, int) switchPair;
            Cost = TSPCost();
            Temperature = 500d;
            double deltaCost;
            while (Cost > OptimalCost) {
                for (int i = 0; Cost > OptimalCost && i < 0x00003FFF; i++) {
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
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            DisplayRoute(TSPLSSACanvas);
                            Bindings.Update();
                        });
                    }
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    DisplayRoute(TSPLSSACanvas);
                    Bindings.Update();
                });
                Temperature *= 0.99;
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPLSSACanvas);
                TSPLSResultTextBlock.Text += "Done!";
            });
        }

        private void CalculateButtom_Click(object sender, RoutedEventArgs e) => Task.Run(() => SimulatedAnnealingTSP());
    }
}
