using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        List<List<int>> Population { get; set; }
        List<List<int>> Offspring { get; set; }
        List<double> CostBuffer { get; set; }
        public TSPGAPage() {
            this.InitializeComponent();
        }

        private List<int> GenerateOffspring(List<int> parent1, List<int> parent2) {
            int seperateStart = Random.Next(0, N - 1);
            int seperateEnd = Random.Next(seperateStart + 1, N);
            List<int> parent2Remains = new List<int>(parent2);
            List<int> answer = new List<int>();
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

            if (Random.NextDouble() < 0.1d) {
                SwapPair(answer, RandomPair());
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

        private void LocalSearchTSP() {
            (int, int) switchPair;
            Cost = TSPCost(CityOrder);
            double deltaCost;
            for (int i = 0; Cost > TargetCost && i < 0x00000FFF; i++) {
                Cost = TSPCost(CityOrder);
                switchPair = RandomPair();
                SwapPair(CityOrder, switchPair);

                deltaCost = TSPCost(CityOrder) - Cost;
                if (deltaCost >= 0) {
                    SwapPair(CityOrder, switchPair);
                }
            }
        }

        private async void Display(List<int> bestAnswer, double minCost) {
            CityOrder = bestAnswer.ToList();
            Cost = minCost;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPCanvas);
                Bindings.Update();
            });
        }

        private async void GeneticAlgorithmTSP() {
            int PopulationSize = N;
            int OffspringSize = 2 * N;// PopulationSize * (PopulationSize - 1) / 2;
            Population = new List<List<int>>();
            Offspring = new List<List<int>>();
            CostBuffer = new List<double>();
            for (int i = 0; i < PopulationSize; i++) {
                CityOrder.Sort((a, b) => Random.Next(0, 3) - 1);
                LocalSearchTSP();
                Population.Add(new List<int>(CityOrder));
            }

            double minCost = Cost, sum;
            List<int> bestAnswer = CityOrder.ToList();
            List<double> scores;

            void UpdateBestAnswer(double cost, List<int> cityOrder) {
                if (cost < minCost) {
                    minCost = cost;
                    bestAnswer = cityOrder.ToList();
                    Display(bestAnswer, minCost);
                }
            }

            void GenerateScores() {
                sum = CostBuffer.Sum();
                scores = CostBuffer.ToList();
                for (int i = 0; i < scores.Count; i++) {
                    scores[i] = sum / scores[i];
                }
            }

            while (minCost > OptimalCost) {
                for (int i = 0; i < PopulationSize; i++) {
                    CostBuffer.Add(TSPCost(Population[i]));
                    UpdateBestAnswer(CostBuffer.Last(), Population[i]);
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
                    nextPopulation.Add(index < PopulationSize ? Population[index].ToList() : Offspring[index - PopulationSize]);
                }
                Population.Clear();
                Offspring.Clear();
                CostBuffer.Clear();
                Population = nextPopulation.ToList();

                CityOrder = bestAnswer.ToList();
                Cost = minCost;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    DisplayRoute(TSPCanvas);
                    Bindings.Update();
                });
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                DisplayRoute(TSPCanvas);
                Bindings.Update();
                TSPLSResultTextBlock.Text += " Done!";
            });
        }

        private void CalculateButtomClick(object sender, RoutedEventArgs e) => Task.Run(() => GeneticAlgorithmTSP());
    }
}
