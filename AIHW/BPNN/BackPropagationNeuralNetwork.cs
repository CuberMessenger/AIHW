using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHW.BPNN {
    class NeuralNode {
        internal float Output { get; set; }
        internal float Bias { get; set; }
        internal float Error { get; set; }
        internal float[] Weights { get; set; }
        internal int Index { get; set; }
        internal FullyConnectedNeuralLayer CurrentLayer { get; set; }
        internal BackPropagationNeuralNetwork CurrentNetwork { get; set; }

        private static Random Random = new Random(System.DateTime.Now.Millisecond);

        private static float RandomFloat() => (float)Random.NextDouble() * 2f - 1f;

        internal NeuralNode(int numOfWeights, FullyConnectedNeuralLayer currentLayer, BackPropagationNeuralNetwork currentNetwork) {
            CurrentLayer = currentLayer;
            CurrentNetwork = currentNetwork;
            if (numOfWeights < 1) {
                return;//InputLayer
            }
            Weights = new float[numOfWeights];
            for (int i = 0; i < numOfWeights; i++) {
                Weights[i] = RandomFloat();
            }
            Bias = RandomFloat();
        }

        internal void Forward(FullyConnectedNeuralLayer previousLayer) {
            Output = Bias;
            for (int i = 0; i < Weights.Length; i++) {
                Output += Weights[i] * previousLayer.Nodes[i].Output;
            }
            Output = CurrentLayer.ActivateFunction(Output);
        }

        internal void Backward(FullyConnectedNeuralLayer nextLayer, FullyConnectedNeuralLayer previousLayer) {
            Error = 0f;
            for (int i = 0; i < nextLayer.Nodes.Length; i++) {
                Error += nextLayer.Nodes[i].Error * nextLayer.Nodes[i].Weights[Index];
            }
            Error *= CurrentLayer.ActivationDerivative(Output);

            Bias -= CurrentNetwork.LearnRate * Error;
            for (int i = 0; i < Weights.Length; i++) {
                Weights[i] -= CurrentNetwork.LearnRate * Error * previousLayer.Nodes[i].Output;
            }
        }

        internal void OutputLayerBackward(float lossFunctionResult, FullyConnectedNeuralLayer previousLayer) {
            Error = lossFunctionResult * CurrentLayer.ActivationDerivative(Output);
            Bias -= CurrentNetwork.LearnRate * Error;
            for (int i = 0; i < Weights.Length; i++) {
                Weights[i] -= CurrentNetwork.LearnRate * Error * previousLayer.Nodes[i].Output;
            }
        }
    }
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
        internal static float ReLuDerivative(float x) => x > 0f ? 1f : 0f;
        internal static float Linear(float x) => x;
        internal static float LinearDerivative(float x) => 1f;

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

        internal static float[] SoftmaxDerivative(float[] x) {
            float[] answer = new float[x.Length];
            float sum = x.Sum();
            for (int i = 0; i < x.Length; i++) {
                float exp = (float)Math.Exp(x[i]);
                answer[i] = exp * (sum - exp) / (sum * sum) + SoftmaxShift;
            }

        }

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
            ActivateFunction = ReLu;
            ActivationDerivative = ReLuDerivative;
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

                Biases[n] -= CurrentNetwork.LearnRate * Errors[n];
                for (int i = 0; i < NumOfWeights; i++) {
                    Weights[n, i] -= CurrentNetwork.LearnRate * Errors[n] * PreviousLayer.Outputs[i];
                }
            }
        }

        internal void OutputLayerForward() {
            for (int n = 0; n < NumOfNodes; n++) {
                Outputs[n] = Biases[n];
                for (int i = 0; i < NumOfWeights; i++) {
                    Outputs[n] += Weights[n, i] * PreviousLayer.Outputs[i];
                }
            }
            SoftmaxInPlace(Outputs);
        }

        internal void OutputLayerBackward(float[] error) {
            //for (int n = 0; n < NumOfNodes; n++) {
            //    Errors[n] = 0f;
            //    for (int i = 0; i < NextLayer.NumOfNodes; i++) {
            //        Errors[n] += NextLayer.Errors[i] * NextLayer.Weights[i, n];
            //    }
            //    Errors[n] *= ActivationDerivative(Outputs[n]);

            //    Biases[n] -= CurrentNetwork.LearnRate * Errors[n];
            //    for (int i = 0; i < NumOfWeights; i++) {
            //        Weights[n, i] -= CurrentNetwork.LearnRate * Errors[n] * PreviousLayer.Outputs[i];
            //    }
            //}
            for (int n = 0; n < NumOfNodes; n++) {
                Errors[n] = error[n] * ActivationDerivative(Outputs[n]);
            }
        }
    }

    class BackPropagationNeuralNetwork {
        internal int Epoch { get; set; }
        internal float LearnRate { get; set; }
        internal FullyConnectedNeuralLayer[] Layers { get; set; }
        internal Func<float[], int, float[]> LossFunction { get; set; }
        internal Func<float[], int, float[]> LossFunctionDerivative { get; set; }

        internal static float[] CrossEntropy(float[] outputs, int target) {
            float[] answer = new float[outputs.Length];
            for (int i = 0; i < outputs.Length; i++) {
                answer[i] = (float)(target == i ? -Math.Log(outputs[i]) : -Math.Log(1f - outputs[i]));
            }
            return answer;
        }

        internal static float[] CrossEntropyDerivative(float[] outputs, int target) {
            float[] answer = new float[outputs.Length];
            for (int i = 0; i < outputs.Length; i++) {
                answer[i] = target == i ? -1f / outputs[i] : 1f / (1 - outputs[i]);
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

            LossFunction = CrossEntropy;
            LossFunctionDerivative = CrossEntropyDerivative;
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

        internal float[] Train(float[,,] trainData, int[] trainLabel) {
            float[] losses = new float[Epoch];
            int numOfInstance = 5000;
            //int numOfInstance = trainData.GetLength(0);
            for (int e = 0; e < Epoch; e++) {
                for (int instanceIndex = 0; instanceIndex < numOfInstance; instanceIndex++) {
                    SetInput(trainData, instanceIndex, Layers.First());

                    //Forward
                    for (int i = 1; i < Layers.Length; i++) {
                        Layers[i].Forward();
                    }

                    var loss = LossFunction(Layers.Last().Outputs, trainLabel[instanceIndex]);
                    losses[e] = loss.Average();
                    var outputError = LossFunctionDerivative(Layers.Last().Outputs, trainLabel[instanceIndex]);

                    //Backward
                    Layers.Last().OutputLayerBackward(outputError);
                    for (int i = Layers.Length - 2; i > 0; i--) {
                        Layers[i].Backward();
                    }
                }
            }

            return losses;
        }

        internal float Test(float[,,] testData, int[] testLabel) {
            float accuracy = 0f;
            int numOfInstance = testData.GetLength(0);
            for (int instanceIndex = 0; instanceIndex < numOfInstance; instanceIndex++) {
                SetInput(testData, instanceIndex, Layers.First());
                for (int i = 1; i < Layers.Length; i++) {
                    Layers[i].Forward();
                }
                float output = Layers.Last().Nodes.First().Output;
                accuracy += Math.Round(output) == testLabel[instanceIndex] ? 1f : 0f;
            }
            accuracy /= numOfInstance;
            return accuracy;
        }
    }
}
