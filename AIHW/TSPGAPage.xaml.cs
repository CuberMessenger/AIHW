using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AIHW {
    public sealed partial class TSPGAPage : TSPBasePage {
        private List<List<int>> Population { get; set; }
        private List<List<int>> Offspring { get; set; }
        private List<double> CostBuffer { get; set; }
        private int PopulationSize { get; set; }
        private int OffspringSize { get; set; }
        private double MinCost { get; set; }
        private int Generation { get; set; }
        public TSPGAPage() {
            this.InitializeComponent();
        }

        private List<int> GenerateOffspring(List<int> parent1, List<int> parent2) {
            List<int> answer = new List<int>();
            if (Random.NextDouble() < 0.5d) {
                int seperateStart = Random.Next(0, N - 1);
                int seperateEnd = Random.Next(seperateStart + 1, N);
                List<int> parent2Remains = new List<int>(parent2);
                for (int i = seperateStart; i <= seperateEnd; i++) {
                    parent2Remains.Remove(parent1[i]);
                }
                for (int i = 0, j = 0; i < N; i++) {
                    if (i >= seperateStart && i <= seperateEnd) {
                        answer.Add(parent1[i]);
                    }
                    else {
                        answer.Add(parent2Remains[j++]);
                    }
                }
            }
            else {
                int seperateIndex = Random.Next(1, N - 1);
                answer = new List<int>(parent1);
                if (Random.NextDouble() < 0.5d) {
                    for (int i = 0; i < seperateIndex; i++) {
                        if (answer[i] == parent2[i]) {
                            continue;
                        }
                        else {
                            SwapPair(answer, (i, answer.IndexOf(parent2[i])));
                        }
                    }
                }
                else {
                    for (int i = seperateIndex - 1; i >= 0; i--) {
                        if (answer[i] == parent2[i]) {
                            continue;
                        }
                        else {
                            SwapPair(answer, (i, answer.IndexOf(parent2[i])));
                        }
                    }
                }
            }
            if (Random.NextDouble() < 0.2d) {
                LocalSearchTSP(answer);
            }
            if (Random.NextDouble() < 0.4d) {
                int seperateStart = Random.Next(0, N - 1);
                int seperateEnd = Random.Next(seperateStart + 1, N);
                answer.Reverse(seperateStart, seperateEnd - seperateStart);
                return answer;
            }
            if (Random.NextDouble() < 0.1d) {
                SwapPair(answer, RandomPair());
                return answer;
            }
            if (Random.NextDouble() < 0.1d) {
                Shuffle(answer);
                LocalSearchTSP(answer);
                return answer;
            }
            return answer;
        }

        private int RandomIndexByCost(List<double> scores) {
            int answer = 0, i = 1;
            double pointer = Random.NextDouble() * scores.Sum();
            scores.Aggregate((a, b) => {
                if (pointer > a) {
                    answer = i;
                }
                i++;
                return a + b;
            });
            return answer;
        }

        private void LocalSearchTSP(List<int> order = null) {
            if (order is null) {
                order = CityOrder;
            }
            double deltaCost;

            for (int i = 0; i < N; i++) {
                Cost = TSPCost(order);
                var switchPair1 = RandomPair();
                var switchPair2 = RandomPair();
                SwapPair(order, switchPair1);
                SwapPair(order, switchPair2);
                deltaCost = TSPCost(order) - Cost;
                if (deltaCost >= 0) {
                    SwapPair(order, switchPair2);
                    SwapPair(order, switchPair1);
                }
            }

            for (int i = 0; i < N; i++) {
                Cost = TSPCost(order);
                var switchPair = RandomPair();
                SwapPair(order, switchPair);
                deltaCost = TSPCost(order) - Cost;
                if (deltaCost >= 0) {
                    SwapPair(order, switchPair);
                }
            }
        }

        private async void GeneticAlgorithmTSP() {
            PopulationSize = N;
            OffspringSize = 5 * N;
            Population = new List<List<int>>();
            Offspring = new List<List<int>>();
            CostBuffer = new List<double>();
            for (int i = 0; i < PopulationSize; i++) {
                Shuffle(CityOrder);
                Population.Add(new List<int>(CityOrder));
            }

            MinCost = Cost;
            List<int> bestAnswer = new List<int>(CityOrder);
            List<double> scores;

            void UpdateBestAnswer(double cost, List<int> cityOrder) {
                if (cost < MinCost) {
                    MinCost = cost;
                    bestAnswer = new List<int>(cityOrder);
                }
            }

            void GenerateScores() {
                List<(int, double)> instance = new List<(int, double)>();
                for (int i = 0; i < CostBuffer.Count; i++) {
                    instance.Add((i, CostBuffer[i]));
                }
                instance = instance.OrderByDescending(x => x.Item2).ToList();

                scores = new List<double>(CostBuffer);
                for (int i = 0; i < instance.Count; i++) {
                    scores[instance[i].Item1] = i + 1;
                }

                //scores = new List<double>(CostBuffer);
                //double minValue = scores.Min();
                //double maxValue = scores.Max();
                //for (int i = 0; i < scores.Count; i++) {
                //    scores[i] = (scores[i] - minValue) / (maxValue - minValue);
                //    scores[i] = 1 - scores[i];
                //}
            }

            Generation = 0;
            while (Generation <= 1000) {
                Generation++;
                for (int i = 0; i < PopulationSize; i++) {
                    CityOrder = Population[i];
                    CostBuffer.Add(TSPCost(Population[i]));
                    UpdateBestAnswer(CostBuffer.Last(), Population[i]);

                    Cost = TSPCost(CityOrder);
                    if (DisplayEveryStep) {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            DisplayRoute(TSPCanvas);
                            Bindings.Update();
                        });
                    }
                }

                GenerateScores();
                for (int i = 0; i < OffspringSize; i++) {
                    var currentOffspring = GenerateOffspring(Population[RandomIndexByCost(scores)], Population[RandomIndexByCost(scores)]);
                    Offspring.Add(currentOffspring);
                    CostBuffer.Add(TSPCost(currentOffspring));
                    UpdateBestAnswer(CostBuffer.Last(), currentOffspring);
                }

                GenerateScores();
                var nextPopulation = new List<List<int>>();
                for (int i = 0, index; i < PopulationSize; i++) {
                    index = RandomIndexByCost(scores);
                    nextPopulation.Add(index < PopulationSize ? new List<int>(Population[index]) : new List<int>(Offspring[index - PopulationSize]));
                }

                Population.Clear();
                Offspring.Clear();
                CostBuffer.Clear();
                Population = new List<List<int>>(nextPopulation);

                CityOrder = new List<int>(bestAnswer);
                Cost = MinCost;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    DisplayRoute(TSPCanvas);
                    Bindings.Update();
                });
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPCanvas);
                Bindings.Update();
                TSPCostTextBlock.Text += " Done!";
            });
        }

        private void CalculateButtomClick(object sender, RoutedEventArgs e) => Task.Run(() => GeneticAlgorithmTSP());
    }
}
