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

        public TSPLSSAPage() : base() {
            this.InitializeComponent();
            DisplayEveryStep = true;
            DataLoadedEvent += TSPLSSAPageDataLoadedEventHandler;
        }

        private void TSPLSSAPageDataLoadedEventHandler(object sender, EventArgs e) => Bindings.Update();

        private async void LocalSearchTSP() {
            Cost = TSPCost(CityOrder);
            double minCost, currentCost;
            List<int> bestNeighbour = new List<int>();
            while (Cost > TargetCost) {
                minCost = double.MaxValue;
                bestNeighbour.Clear();

                for (int i = 0; i < N; i++) {
                    int seperateStart = Random.Next(0, N - 1);
                    int seperateEnd = Random.Next(seperateStart + 1, N);
                    CityOrder.Reverse(seperateStart, seperateEnd - seperateStart);
                    currentCost = TSPCost(CityOrder);
                    if (currentCost < minCost) {
                        minCost = currentCost;
                        bestNeighbour = CityOrder.ToList();
                    }
                    CityOrder.Reverse(seperateStart, seperateEnd - seperateStart);
                }

                ////Random switch four
                //for (int i = 0; i < N * N; i++) {
                //    var switchPair1 = RandomPair();
                //    var switchPair2 = RandomPair();
                //    SwapPair(CityOrder, switchPair1);
                //    SwapPair(CityOrder, switchPair2);
                //    currentCost = TSPCost(CityOrder);
                //    if (currentCost < minCost) {
                //        minCost = currentCost;
                //        bestNeighbour = CityOrder.ToList();
                //    }
                //    SwapPair(CityOrder, switchPair2);
                //    SwapPair(CityOrder, switchPair1);
                //}

                //Switch two
                for (int i = 0; i < N; i++) {
                    for (int j = i + 1; j < N; j++) {
                        SwapPair(CityOrder, (i, j));
                        currentCost = TSPCost(CityOrder);
                        if (currentCost < minCost) {
                            minCost = currentCost;
                            bestNeighbour = CityOrder.ToList();
                        }
                        SwapPair(CityOrder, (i, j));
                    }
                }

                //Check Dead End
                if (minCost == Cost) {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        TSPCostTextBlock.Text += " DeadEnd X_X";
                    });
                    return;
                }

                CityOrder = bestNeighbour.ToList();
                Cost = TSPCost(CityOrder);
                if (DisplayEveryStep) {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        DisplayRoute(TSPCanvas);
                        Bindings.Update();
                    });
                }
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPCanvas);
                TSPCostTextBlock.Text += " Done!";
            });
        }

        private async void SimulatedAnnealingLocalSearchTSP() {
            (int, int) switchPair;
            Cost = TSPCost(CityOrder);
            Temperature = 100d;
            double deltaCost;
            while (Temperature >= 1d) {
                for (int i = 0; i < 0x0000FFFF; i++) {
                    //SA
                    Cost = TSPCost(CityOrder);
                    int seperateStart = Random.Next(0, N - 1);
                    int seperateEnd = Random.Next(seperateStart + 1, N);
                    CityOrder.Reverse(seperateStart, seperateEnd - seperateStart);
                    switchPair = RandomPair();
                    SwapPair(CityOrder, switchPair);

                    deltaCost = TSPCost(CityOrder) - Cost;
                    if (deltaCost >= 0 && (Random.NextDouble() >= Math.Exp(-(deltaCost / Temperature)))) {
                        SwapPair(CityOrder, switchPair);
                        CityOrder.Reverse(seperateStart, seperateEnd - seperateStart);
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
                Bindings.Update();
                TSPCostTextBlock.Text += " Done!";
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
