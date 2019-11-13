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
            return null;
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

        private async void GeneticAlgorithmTSP() {
            Population = new List<List<int>>();
            Offspring = new List<List<int>>();
            CostBuffer = new List<double>();
            for (int i = 0; i < N; i++) {
                Population.Add(CityOrder.OrderBy(a => Random.Next(0, 3) - 1).ToList());
            }

            double minCost = Cost, sum;
            List<int> bestAnswer = CityOrder.ToList();
            List<double> scores;

            while (minCost > OptimalCost) {
                for (int i = 0; i < N; i++) {
                    CityOrder = Population[i].ToList();
                    CostBuffer.Add(TSPCost());
                }

                if (CostBuffer.Min() < minCost) {
                    minCost = CostBuffer.Min();
                    bestAnswer = Population[CostBuffer.IndexOf(minCost)].ToList();
                }

                sum = CostBuffer.Sum();
                scores = CostBuffer.GetRange(0, N).ToList();
                scores.ForEach(x => { x = sum - x; });
                for (int i = 0; i < N; i++) {
                    var currentOffspring = GenerateOffspring(Population[RandomIndexByCost(scores)], Population[RandomIndexByCost(scores)]);
                    Offspring.Add(currentOffspring);

                    CityOrder = currentOffspring.ToList();
                    CostBuffer.Add(TSPCost());
                }

                sum = CostBuffer.Sum();
                scores = CostBuffer.GetRange(0, 2 * N).ToList();
                scores.ForEach(x => { x = sum - x; });
                var nextPopulation = new List<List<int>>();
                for (int i = 0, index; i < N; i++) {
                    index = RandomIndexByCost(scores);
                    nextPopulation.Add(index < N ? Population[index].ToList() : Offspring[index % N]);
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

        }

        private void CalculateButtomClick(object sender, RoutedEventArgs e) => Task.Run(() => GeneticAlgorithmTSP());
    }
}
