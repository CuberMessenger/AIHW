﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace AIHW.CNN {
    class ConvolutionalLayer {
        internal int NumOfKernal { get; set; }
        internal (int, int, int) KernalShape { get; set; }
        internal (int, int, int) InputShape { get; set; }
        internal (int, int, int) OutputShape { get; set; }
        internal float[][,,] Kernals { get; set; }
        internal float[][,,] DeltaKernals { get; set; }
        internal float[] Biases { get; set; }
        internal float[] DeltaBiases { get; set; }
        internal float[,,] Input { get; set; }
        internal float[,,] DeltaInput { get; set; }
        internal float[,,] FeatureMap { get; set; }
        internal float[,,] DeltaFeatureMap { get; set; }
        internal Func<float, float> ActivationFunction { get; set; }
        internal Func<float, float> ActivationDerivative { get; set; }

        private static Random Random = new Random(System.DateTime.Now.Millisecond);

        private static float RandomFloat() => (float)Random.NextDouble() * 2f - 1f;

        internal static float ReLu(float x) => x > 0f ? x : 0f;
        internal static float ReLuDerivative(float relu) => relu > 0f ? 1f : 0f;
        internal static float Sigmoid(float x) => 1f / (1f + (float)Math.Exp(-x));
        internal static float SigmoidDerivative(float sigmoid) => sigmoid * (1f - sigmoid);

        internal ConvolutionalLayer(int numOfKernal, (int, int) kernalShape, (int, int, int) inputShape) {
            NumOfKernal = numOfKernal;
            KernalShape = (inputShape.Item1, kernalShape.Item1, kernalShape.Item2);//[Channel, Height, Width]
            InputShape = inputShape;
            OutputShape = (NumOfKernal, InputShape.Item2 - KernalShape.Item2 + 1, InputShape.Item3 - KernalShape.Item3 + 1);

            Kernals = new float[NumOfKernal][,,];
            DeltaKernals = new float[NumOfKernal][,,];
            for (int k = 0; k < numOfKernal; k++) {
                Kernals[k] = new float[KernalShape.Item1, KernalShape.Item2, KernalShape.Item3];
                DeltaKernals[k] = new float[KernalShape.Item1, KernalShape.Item2, KernalShape.Item3];
                for (int ch = 0; ch < KernalShape.Item1; ch++) {
                    for (int r = 0; r < KernalShape.Item2; r++) {
                        for (int c = 0; c < KernalShape.Item3; c++) {
                            Kernals[k][ch, r, c] = RandomFloat();
                        }
                    }
                }
            }

            Biases = new float[numOfKernal];
            DeltaBiases = new float[numOfKernal];
            for (int k = 0; k < numOfKernal; k++) {
                Biases[k] = RandomFloat();
            }

            Input = new float[InputShape.Item1, InputShape.Item2, InputShape.Item3];
            DeltaInput = new float[InputShape.Item1, InputShape.Item2, InputShape.Item3];

            FeatureMap = new float[OutputShape.Item1, OutputShape.Item2, OutputShape.Item3];
            DeltaFeatureMap = new float[OutputShape.Item1, OutputShape.Item2, OutputShape.Item3];

            ActivationFunction = Sigmoid;
            ActivationDerivative = SigmoidDerivative;
        }

        internal float Convolution(float[,,] input, float[,,] kernal, int startRow, int startColumn, int channel) {
            float answer = 0f;
            for (int r = 0; r < kernal.GetLength(1); r++) {
                for (int c = 0; c < kernal.GetLength(2); c++) {
                    answer += input[channel, r + startRow, c + startColumn] * kernal[channel, r, c];
                }
            }
            return answer;
        }

        internal void FullyConvolution(float[,,] input, float[,,] kernal, float[,,] answer, int indexOfInput) {
            //input is un-padded DeltaDeatureMap
            //kernal is current Kernal
            //answer is DeltaInput
            var kernalShape = (kernal.GetLength(0), kernal.GetLength(1), kernal.GetLength(2));
            var rotatedKernel = new float[kernalShape.Item1, kernalShape.Item2, kernalShape.Item3];
            for (int ch = 0; ch < kernalShape.Item1; ch++) {
                for (int r = 0; r < kernalShape.Item2; r++) {
                    for (int c = 0; c < kernalShape.Item3; c++) {
                        rotatedKernel[ch, r, c] = kernal[ch, kernalShape.Item2 - 1 - r, kernalShape.Item3 - 1 - c];
                    }
                }
            }

            var paddingShape = ((kernalShape.Item2 - 1) / 2, (kernalShape.Item3 - 1) / 2);
            var inputShape = (input.GetLength(0), input.GetLength(1), input.GetLength(2));
            var paddedInput = new float[inputShape.Item2 + (kernalShape.Item2 - 1) * 2, inputShape.Item3 + (kernalShape.Item3 - 1) * 2];
            for (int r = 0; r < paddedInput.GetLength(0); r++) {
                for (int c = 0; c < paddedInput.GetLength(1); c++) {
                    paddedInput[r, c] = 0f;
                }
            }
            for (int r = 0; r < inputShape.Item2; r++) {
                for (int c = 0; c < inputShape.Item3; c++) {
                    paddedInput[r + paddingShape.Item1, c + paddingShape.Item2] = input[indexOfInput, r, c];
                }
            }

            for (int ch = 0; ch < InputShape.Item1; ch++) {
                for (int r = 0; r < InputShape.Item2; r++) {
                    for (int c = 0; c < InputShape.Item3; c++) {
                        for (int kr = 0; kr < kernalShape.Item2; kr++) {
                            for (int kc = 0; kc < kernalShape.Item3; kc++) {
                                answer[ch, r, c] += paddedInput[r + kr, c + kc] * rotatedKernel[ch, kr, kc];
                            }
                        }
                    }
                }
            }
        }

        internal float[,,] Forward(float[,,] input) {
            Input = input;
            for (int k = 0; k < NumOfKernal; k++) {
                for (int ch = 0; ch < KernalShape.Item1; ch++) {
                    for (int r = 0; r < OutputShape.Item2; r++) {
                        for (int c = 0; c < OutputShape.Item3; c++) {
                            FeatureMap[k, r, c] = ActivationFunction(Convolution(Input, Kernals[k], r, c, ch) + Biases[k]);
                        }
                    }
                }
            }
            return FeatureMap;
        }

        internal float[,,] Backward(float[,,] deltaFeatureMap) {
            //dY is gradient of current layer output [Channel(NumOfKernal), Height, Width]
            for (int ch = 0; ch < OutputShape.Item1; ch++) {
                for (int r = 0; r < OutputShape.Item2; r++) {
                    for (int c = 0; c < OutputShape.Item3; c++) {
                        deltaFeatureMap[ch, r, c] = deltaFeatureMap[ch, r, c] * ActivationDerivative(FeatureMap[ch, r, c]);
                    }
                }
            }
            DeltaFeatureMap = deltaFeatureMap;

            //DeltaBiases, for update Biases. float[k]
            for (int k = 0; k < NumOfKernal; k++) {
                DeltaBiases[k] = 0f;
                for (int r = 0; r < OutputShape.Item2; r++) {
                    for (int c = 0; c < OutputShape.Item3; c++) {
                        DeltaBiases[k] += deltaFeatureMap[k, r, c];
                    }
                }
            }

            //DeltaKernals, for update Kernals. float[k][ch, r, c]
            for (int k = 0; k < NumOfKernal; k++) {
                for (int ch = 0; ch < KernalShape.Item1; ch++) {
                    for (int r = 0; r < KernalShape.Item2; r++) {
                        for (int c = 0; c < KernalShape.Item3; c++) {
                            //DeltaKernals[k][ch, r, c] = Convolution(Input, DeltaFeatureMap, r, c, ch);
                            DeltaKernals[k][ch, r, c] = 0f;
                            for (int kr = 0; kr < OutputShape.Item2; kr++) {
                                for (int kc = 0; kc < OutputShape.Item3; kc++) {
                                    DeltaKernals[k][ch, r, c] += Input[ch, r, c] * DeltaFeatureMap[k, kr, kc];
                                }
                            }
                        }
                    }
                }
            }

            //DeltaInput, back propagate to privious layer. float[ch, r, c]
            for (int ch = 0; ch < InputShape.Item1; ch++) {
                for (int r = 0; r < InputShape.Item2; r++) {
                    for (int c = 0; c < InputShape.Item3; c++) {
                        DeltaInput[ch, r, c] = 0f;
                    }
                }
            }
            for (int k = 0; k < NumOfKernal; k++) {
                FullyConvolution(DeltaFeatureMap, Kernals[k], DeltaInput, k);
            }

            return DeltaInput;
        }

        internal void UpdateWeights(float learnRate) {
            //Biases
            for (int k = 0; k < NumOfKernal; k++) {
                Biases[k] += learnRate * DeltaBiases[k];
            }

            //Kernals
            for (int k = 0; k < NumOfKernal; k++) {
                for (int ch = 0; ch < KernalShape.Item1; ch++) {
                    for (int r = 0; r < KernalShape.Item2; r++) {
                        for (int c = 0; c < KernalShape.Item3; c++) {
                            Kernals[k][ch, r, c] += learnRate * DeltaKernals[k][ch, r, c];
                        }
                    }
                }
            }
        }
    }

    class ConvolutionalNeuralNetwork {
        internal int Epoch { get; set; }
        internal float LearnRate { get; set; }
        internal float[] Losses { get; set; }
        internal (int, int, int) InputShape { get; set; }
        internal ConvolutionalLayer[] Layers { get; set; }
        internal Func<float[], int, float[]> LossFunction { get; set; }
        internal Func<float[], int, float[]> LossFunctionDerivative { get; set; }

        internal static float[] CrossEntropy(float[] outputs, int target) {
            float[] answer = new float[outputs.Length];
            for (int i = 0; i < outputs.Length; i++) {
                answer[i] = (float)(target == i ? -Math.Log(outputs[i] + 0.0001f) : -Math.Log(1f - outputs[i] + 0.0001f));
            }
            return answer;
        }

        internal static float[] CrossEntropyDerivative(float[] outputs, int target) {
            float[] answer = new float[outputs.Length];
            for (int i = 0; i < answer.Length; i++) {
                if (target == i) {
                    answer[i] = -1f / (outputs[i] + 0.0001f);
                }
                else {
                    answer[i] = 1f / (1f - outputs[i] + 0.0001f);
                }
            }
            return answer;
        }

        internal static void DeepCopy(ref float[,,] source, ref float[,,] destination) {
            for (int i = 0; i < source.GetLength(0); i++) {
                for (int j = 0; j < source.GetLength(1); j++) {
                    for (int k = 0; k < source.GetLength(2); k++) {
                        destination[i, j, k] = source[i, j, k];
                    }
                }
            }
        }

        internal ConvolutionalNeuralNetwork((int, int, int)[] networkShape, (int, int, int) inputShape, int epoch, float learnRate) {
            //networkShape (kernalSize.1, kernalSize.2, numOfKernal)
            Epoch = epoch;
            LearnRate = learnRate;
            Losses = new float[epoch];
            InputShape = inputShape;
            for (int i = 0; i < epoch; i++) {
                Losses[i] = 0f;
            }

            LossFunction = CrossEntropy;
            LossFunctionDerivative = CrossEntropyDerivative;

            Layers = new ConvolutionalLayer[networkShape.Length];
            Layers[0] = new ConvolutionalLayer(networkShape[0].Item3,
                    (networkShape[0].Item1, networkShape[0].Item2),
                    inputShape);
            for (int i = 1; i < Layers.Length; i++) {
                Layers[i] = new ConvolutionalLayer(networkShape[i].Item3,
                    (networkShape[i].Item1, networkShape[i].Item2),
                    Layers[i - 1].OutputShape);
            }
        }

        internal async void Train(float[][,,] trainData, int[] trainLabel, CoreDispatcher coreDispatcher, TextBlock textBlock) {
            float[] delta = null;
            //int numOfInstance = 10000;
            int numOfInstance = trainData.Length;
            for (int e = 0; e < Epoch; e++) {
                for (int instanceIndex = 0; instanceIndex < numOfInstance; instanceIndex++) {
                    //Forward
                    var previousOutput = new float[InputShape.Item1, InputShape.Item2, InputShape.Item3];
                    DeepCopy(ref trainData[instanceIndex], ref previousOutput);
                    foreach (var layer in Layers) {
                        previousOutput = layer.Forward(previousOutput);
                    }

                    //previousOutput is output of the net now
                    //shape is 10*1*1
                    var outputs = new float[previousOutput.GetLength(0)];
                    for (int i = 0; i < outputs.Length; i++) {
                        outputs[i] = previousOutput[i, 0, 0];
                    }
                    var loss = LossFunction(outputs, trainLabel[instanceIndex]);
                    Losses[e] += loss.Average();

                    delta = LossFunctionDerivative(outputs, trainLabel[instanceIndex]);

                    //Backward
                    var gradient = new float[outputs.Length, 1, 1];
                    for (int i = 0; i < gradient.Length; i++) {
                        gradient[i, 0, 0] = delta[i];
                    }
                    for (int i = Layers.Length - 1; i >= 0; i--) {
                        gradient = Layers[i].Backward(gradient);
                    }

                    //Update
                    foreach (var layer in Layers) {
                        layer.UpdateWeights(LearnRate);
                    }

                    if (instanceIndex % 1000 == 0) {
                        await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            textBlock.Text = $"Epoch: {e}, Loss: {Losses[e] / instanceIndex}";
                        });
                    }
                }
            }
            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                textBlock.Text = $"TrainDone Loss: {Losses.Last() / numOfInstance}";
            });
        }

        internal float Test(float[][,,] testData, int[] testLabel) {
            float accuracy = 0f;
            int numOfInstance = testData.Length;
            for (int instanceIndex = 0; instanceIndex < numOfInstance; instanceIndex++) {
                accuracy += TestOne(testData[instanceIndex]) == testLabel[instanceIndex] ? 1f : 0f;
            }
            accuracy /= numOfInstance;
            return accuracy;
        }

        internal int TestOne(float[,,] testData) {
            var previousOutput = new float[InputShape.Item1, InputShape.Item2, InputShape.Item3];
            DeepCopy(ref testData, ref previousOutput);
            foreach (var layer in Layers) {
                previousOutput = layer.Forward(previousOutput);
            }
            var outputs = new List<float>();
            for (int i = 0; i < previousOutput.GetLength(0); i++) {
                outputs.Add(previousOutput[i, 0, 0]);
            }
            return outputs.IndexOf(outputs.Max());
        }
    }
}
