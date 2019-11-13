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
        private bool UseSimulatedAnnealing { get; set; }
        private double Temperature { get; set; }
        private bool DisplayEveryStep { get; set; }

        public TSPLSSAPage() : base() {
            this.InitializeComponent();
            DisplayEveryStep = true;
            DataLoadedEvent += TSPLSSAPageDataLoadedEventHandler;

            TargetCost = OptimalCost * 1.05d;
        }

        private void TSPLSSAPageDataLoadedEventHandler(object sender, EventArgs e) => Bindings.Update();

        private (int, int) RandomPair() => (Random.Next(0, N), Random.Next(0, N));

        private void SwapPair((int, int) switchPair) {
            int temp = CityOrder[switchPair.Item1];
            CityOrder[switchPair.Item1] = CityOrder[switchPair.Item2];
            CityOrder[switchPair.Item2] = temp;
        }

        private async void LocalSearchTSP() {
            (int, int) switchPair;
            Cost = TSPCost();
            double minCost, currentCost;
            List<int> bestNeighbour = new List<int>();
            while (Cost > TargetCost) {
                minCost = double.MaxValue;
                bestNeighbour.Clear();
                //Switch two
                for (int i = 0; i < N; i++) {
                    for (int j = i + 1; j < N; j++) {
                        SwapPair((i, j));
                        currentCost = TSPCost();
                        if (currentCost < minCost) {
                            minCost = currentCost;
                            bestNeighbour = CityOrder.ToList();
                        }
                        SwapPair((i, j));
                    }
                }
                //Random switch four
                for (int i = 0; i < N * N; i++) {
                    var switchPair1 = RandomPair();
                    var switchPair2 = RandomPair();
                    SwapPair(switchPair1);
                    SwapPair(switchPair2);
                    currentCost = TSPCost();
                    if (currentCost < minCost) {
                        minCost = currentCost;
                        bestNeighbour = CityOrder.ToList();
                    }
                    SwapPair(switchPair2);
                    SwapPair(switchPair1);
                }

                //Check Dead End
                if (minCost == Cost) {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        TSPLSResultTextBlock.Text += " DeadEnd X_X";
                    });
                    return;
                }

                CityOrder.Clear();
                CityOrder = bestNeighbour.ToList();
                Cost = TSPCost();
                if (DisplayEveryStep) {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        DisplayRoute(TSPCanvas);
                        Bindings.Update();
                    });
                }
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPCanvas);
                TSPLSResultTextBlock.Text += " Done!";
            });
        }

        private async void SimulatedAnnealingLocalSearchTSP() {
            (int, int) switchPair;
            Cost = TSPCost();
            Temperature = 200d;
            double deltaCost;
            while (Cost > TargetCost || Temperature >= 1d) {
                for (int i = 0; Cost > TargetCost && i < 0x0000FFFF; i++) {
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
                            DisplayRoute(TSPCanvas);
                            Bindings.Update();
                        });
                    }
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    DisplayRoute(TSPCanvas);
                    Bindings.Update();
                });
                Temperature *= 0.99;
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPCanvas);
                TSPLSResultTextBlock.Text += " Done!";
            });
        }

        private void CalculateButtomClick(object sender, RoutedEventArgs e) {
            if (UseSimulatedAnnealing) {
                Task.Run(() => SimulatedAnnealingLocalSearchTSP());
            }
            else {
                Task.Run(() => LocalSearchTSP());
            }
        }
    }
}
