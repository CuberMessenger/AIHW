using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace AIHW.BPNN {
    class FullyConnectedNeuralLayer {
        internal int NumOfNodes { get; set; }
        internal int NumOfWeights { get; set; }
        internal float[] Outputs { get; set; }
        internal float[] Biases { get; set; }
        internal float[] Errors { get; set; }
        internal float[,] Weights { get; set; }
        internal BackPropagationNeuralNetwork CurrentNetwork { get; set; }
        internal FullyConnectedNeuralLayer PreviousLayer { get; set; }
        internal FullyConnectedNeuralLayer NextLayer { get; set; }
        internal Func<float, float> ActivateFunction { get; set; }
        internal Func<float, float> ActivationDerivative { get; set; }

        internal static float ReLu(float x) => x > 0f ? x : 0f;
        internal static float ReLuDerivative(float relu) => relu > 0f ? 1f : 0f;
        internal static float Linear(float x) => x;
        internal static float LinearDerivative(float linear) => 1f;

        internal static float SoftmaxShift { get; set; }
        internal static void SoftmaxInPlace(float[] x) {
            SoftmaxShift = x.Max();
            for (int i = 0; i < x.Length; i++) {
                x[i] = (float)Math.Exp(x[i] - SoftmaxShift);
            }
            float sum = x.Sum();
            for (int i = 0; i < x.Length; i++) {
                x[i] /= sum;
            }
        }

        internal static float SoftmaxDerivative(float softmax) => softmax * (1f - softmax);

        internal static float Sigmoid(float x) => 1f / (1f + (float)Math.Exp(-x));

        internal static float SigmoidDerivative(float sigmoid) => sigmoid * (1f - sigmoid);

        private static Random Random = new Random(System.DateTime.Now.Millisecond);

        private static float RandomFloat() => (float)Random.NextDouble() * 2f - 1f;

        internal FullyConnectedNeuralLayer(int numOfNodes, int numOfWeights, BackPropagationNeuralNetwork currentNetwork) {
            NumOfNodes = numOfNodes;
            NumOfWeights = numOfWeights;
            Outputs = new float[numOfNodes];
            Biases = new float[numOfNodes];
            Errors = new float[numOfNodes];
            Weights = new float[numOfNodes, numOfWeights];
            for (int i = 0; i < numOfNodes; i++) {
                Outputs[i] = RandomFloat();
                Biases[i] = RandomFloat();
                Errors[i] = RandomFloat();
                for (int j = 0; j < numOfWeights; j++) {
                    Weights[i, j] = RandomFloat();
                }
            }
            CurrentNetwork = currentNetwork;
            PreviousLayer = null;
            NextLayer = null;
            ActivateFunction = Sigmoid;
            ActivationDerivative = SigmoidDerivative;
        }

        internal void Forward() {
            for (int n = 0; n < NumOfNodes; n++) {
                Outputs[n] = Biases[n];
                for (int i = 0; i < NumOfWeights; i++) {
                    Outputs[n] += Weights[n, i] * PreviousLayer.Outputs[i];
                }
                Outputs[n] = ActivateFunction(Outputs[n]);
            }
        }

        internal void Backward() {
            for (int n = 0; n < NumOfNodes; n++) {
                Errors[n] = 0f;
                for (int i = 0; i < NextLayer.NumOfNodes; i++) {
                    Errors[n] += NextLayer.Errors[i] * NextLayer.Weights[i, n];
                }
                Errors[n] *= ActivationDerivative(Outputs[n]);

                Biases[n] += CurrentNetwork.LearnRate * Errors[n];
                for (int i = 0; i < NumOfWeights; i++) {
                    Weights[n, i] += CurrentNetwork.LearnRate * Errors[n] * PreviousLayer.Outputs[i];
                }
            }
        }

        internal void OutputLayerBackward(float[] delta) {
            for (int n = 0; n < NumOfNodes; n++) {
                Errors[n] = delta[n] * SigmoidDerivative(Outputs[n]);

                Biases[n] += CurrentNetwork.LearnRate * Errors[n];
                for (int i = 0; i < NumOfWeights; i++) {
                    Weights[n, i] += CurrentNetwork.LearnRate * Errors[n] * PreviousLayer.Outputs[i];
                }
            }
        }
    }

    class BackPropagationNeuralNetwork {
        internal int Epoch { get; set; }
        internal float LearnRate { get; set; }
        internal float[] Losses { get; set; }
        internal FullyConnectedNeuralLayer[] Layers { get; set; }
        internal Func<float[], int, float[]> LossFunction { get; set; }

        internal static float[] CrossEntropy(float[] outputs, int target) {
            float[] answer = new float[outputs.Length];
            for (int i = 0; i < outputs.Length; i++) {
                answer[i] = (float)(target == i ? -Math.Log(outputs[i] + 0.0001f) : -Math.Log(1f - outputs[i] + 0.0001f));
            }
            return answer;
        }

        internal BackPropagationNeuralNetwork(int[] networkShape, int epoch, float learnRate) {
            Layers = new FullyConnectedNeuralLayer[networkShape.Length];
            Layers[0] = new FullyConnectedNeuralLayer(networkShape[0], 0, this);
            for (int i = 1; i < networkShape.Length - 1; i++) {
                Layers[i] = new FullyConnectedNeuralLayer(networkShape[i], networkShape[i - 1], this);
            }
            Layers[Layers.Length - 1] = new FullyConnectedNeuralLayer(networkShape[Layers.Length - 1], networkShape[Layers.Length - 2], this);

            Layers[0].NextLayer = Layers[1];
            Layers[Layers.Length - 1].PreviousLayer = Layers[Layers.Length - 2];
            for (int i = 1; i < Layers.Length - 1; i++) {
                Layers[i].PreviousLayer = Layers[i - 1];
                Layers[i].NextLayer = Layers[i + 1];
            }
            Epoch = epoch;
            LearnRate = learnRate;
            Losses = new float[epoch];
            for (int i = 0; i < epoch; i++) {
                Losses[i] = 0f;
            }

            LossFunction = CrossEntropy;
        }

        internal void SetInput(float[,,] data, int instanceIndex, FullyConnectedNeuralLayer layer) {
            int sideLength = data.GetLength(1);
            for (int i = 0, r = 0, c = 0; i < layer.NumOfNodes; i++) {
                layer.Outputs[i] = data[instanceIndex, r, c];
                c++;
                if (c == sideLength) {
                    c = 0;
                    r++;
                }
            }
        }

        internal async void Train(float[,,] trainData, int[] trainLabel, CoreDispatcher coreDispatcher, TextBlock textBlock) {
            float[] delta = new float[Layers.Last().NumOfNodes];
            int numOfInstance = 10000;
            //int numOfInstance = trainData.GetLength(0);
            for (int e = 0; e < Epoch; e++) {
                for (int instanceIndex = 0; instanceIndex < numOfInstance; instanceIndex++) {
                    SetInput(trainData, instanceIndex, Layers.First());

                    //Forward
                    for (int i = 1; i < Layers.Length; i++) {
                        Layers[i].Forward();
                    }

                    var loss = LossFunction(Layers.Last().Outputs, trainLabel[instanceIndex]);
                    Losses[e] += loss.Average();

                    for (int i = 0; i < delta.Length; i++) {
                        delta[i] = trainLabel[instanceIndex] == i ? 1f - Layers.Last().Outputs[i] : -Layers.Last().Outputs[i];
                    }

                    //Backward
                    Layers.Last().OutputLayerBackward(delta);
                    for (int i = Layers.Length - 2; i > 0; i--) {
                        Layers[i].Backward();
                    }
                }
                await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    textBlock.Text = $"Epoch: {e}, Loss: {Losses[e]}";
                });
            }
            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                textBlock.Text = "TrainDone";
            });
        }

        internal float Test(float[,,] testData, int[] testLabel) {
            float accuracy = 0f;
            int numOfInstance = testData.GetLength(0);
            for (int instanceIndex = 0; instanceIndex < numOfInstance; instanceIndex++) {
                SetInput(testData, instanceIndex, Layers.First());
                for (int i = 1; i < Layers.Length; i++) {
                    Layers[i].Forward();
                }
                var output = Layers.Last().Outputs.ToList();
                accuracy += output.IndexOf(output.Max()) == testLabel[instanceIndex] ? 1f : 0f;
            }
            accuracy /= numOfInstance;
            return accuracy;
        }
    }
}
