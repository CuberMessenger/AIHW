using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHW.CNN {
    class ConvolutionalLayer {
        internal int NumOfKernal { get; set; }
        internal (int, int, int) KernalSize { get; set; }
        internal (int, int, int) InputSize { get; set; }
        internal (int, int, int) OutputSize { get; set; }
        internal float[][,,] Kernals { get; set; }
        internal float[,,] Biases { get; set; }
        internal float[,,] Input { get; set; }
        internal float[,,] FeatureMap { get; set; }
        internal Func<float, float> ActivationFunction { get; set; }
        internal Func<float, float> ActivationDerivative { get; set; }

        private static Random Random = new Random(System.DateTime.Now.Millisecond);

        private static float RandomFloat() => (float)Random.NextDouble() * 2f - 1f;

        internal static float ReLu(float x) => x > 0f ? x : 0f;
        internal static float ReLuDerivative(float relu) => relu > 0f ? 1f : 0f;
        internal static float Sigmoid(float x) => 1f / (1f + (float)Math.Exp(-x));
        internal static float SigmoidDerivative(float sigmoid) => sigmoid * (1f - sigmoid);

        internal ConvolutionalLayer(int numOfKernal, (int, int) kernalSize, (int, int, int) inputSize) {
            NumOfKernal = numOfKernal;
            KernalSize = (kernalSize.Item1, kernalSize.Item2, inputSize.Item3);
            InputSize = inputSize;

            Kernals = new float[numOfKernal][,,];
            for (int i = 0; i < numOfKernal; i++) {
                Kernals[i] = new float[kernalSize.Item1, kernalSize.Item2, inputSize.Item3];
                for (int r = 0; r < kernalSize.Item1; r++) {
                    for (int c = 0; c < kernalSize.Item2; c++) {
                        for (int ch = 0; ch < inputSize.Item3; ch++) {
                            Kernals[i][r, c, ch] = RandomFloat();
                        }
                    }
                }
            }

            OutputSize = (inputSize.Item1 - kernalSize.Item1 + 1, inputSize.Item2 - kernalSize.Item2 + 1, numOfKernal);

            FeatureMap = new float[OutputSize.Item1, OutputSize.Item2, numOfKernal];
            Biases = new float[OutputSize.Item1, OutputSize.Item2, numOfKernal];
            for (int r = 0; r < OutputSize.Item1; r++) {
                for (int c = 0; c < OutputSize.Item2; c++) {
                    for (int ch = 0; ch < numOfKernal; ch++) {
                        Biases[r, c, ch] = RandomFloat();
                    }
                }
            }

            ActivationFunction = ReLu;
            ActivationDerivative = ReLuDerivative;
        }

        internal float Convolution(float[,,] input, float[,,] kernal, int startRow, int startColumn, int channel) {
            float answer = 0f;
            for (int r = startRow; r < startRow + KernalSize.Item1; r++) {
                for (int c = startColumn; c < startColumn + KernalSize.Item2; c++) {
                    answer += input[r, c, channel] * kernal[r - startRow, c - startColumn, channel];
                }
            }
            return answer;
        }

        internal float[,,] FullyConvolution(float[,,] input, float[,,] kernal) {
            var kernalSize = (kernal.GetLength(0), kernal.GetLength(1), kernal.GetLength(2));
            var rotatedKernel = new float[kernalSize.Item1, kernalSize.Item2, kernalSize.Item3];
            for (int k = 0; k < kernalSize.Item3; k++) {
                for (int r = 0; r < kernalSize.Item1; r++) {
                    for (int c = 0; c < kernalSize.Item2; c++) {
                        rotatedKernel[r, c, k] = kernal[kernalSize.Item1 - 1 - r, kernalSize.Item2 - 1 - c, k];
                    }
                }
            }

            var paddedInput =
                new float[input.GetLength(0) + kernalSize.Item1 - 1, input.GetLength(1) + kernalSize.Item2 - 1, input.GetLength(2)];
            for (int k = 0; k < input.GetLength(2); k++) {
                for (int r = 0; r < paddedInput.GetLength(0); r++) {
                    for (int c = 0; c < paddedInput.GetLength(1); c++) {
                        if (r == 0 || c == 0 || r == paddedInput.GetLength(0) - 1 || c == paddedInput.GetLength(1) - 1) {
                            paddedInput[r, c, k] = 0f;
                        }
                        else {
                            paddedInput[r, c, k] = input[r + (kernalSize.Item1 - 1) / 2, c + (kernalSize.Item2 - 1) / 2, k];
                        }
                    }
                }
            }

            var answer = new float[InputSize.Item1, InputSize.Item2, InputSize.Item3];
            for (int k = 0; k < InputSize.Item3; k++) {
                for (int r = 0; r < InputSize.Item1; r++) {
                    for (int c = 0; c < InputSize.Item2; c++) {
                        answer[r, c, k] = Convolution(paddedInput, rotatedKernel, r, c, k);
                    }
                }
            }

            return answer;
        }

        internal float[,,] Forward(float[,,] input) {
            Input = input;
            for (int r = 0; r < OutputSize.Item1; r++) {
                for (int c = 0; c < OutputSize.Item2; c++) {
                    for (int k = 0; k < NumOfKernal; k++) {
                        FeatureMap[r, c, k] = Biases[r, c, k];
                        for (int ch = 0; ch < InputSize.Item3; ch++) {
                            FeatureMap[r, c, k] += Convolution(input, Kernals[k], r, c, ch);
                        }
                        FeatureMap[r, c, k] = ActivationFunction(FeatureMap[r, c, k]);
                    }
                }
            }
            return FeatureMap;
        }

        internal void Backward(float[,,] deltas) {
            var dK = new float[NumOfKernal][,,];
            for (int k = 0; k < NumOfKernal; k++) {
                dK[k] = new float[KernalSize.Item1, KernalSize.Item2, KernalSize.Item3];
                for (int r = 0; r < KernalSize.Item1; r++) {
                    for (int c = 0; c < KernalSize.Item2; c++) {
                        for (int ch = 0; ch < InputSize.Item3; ch++) {
                            dK[k][r, c, ch] = Convolution(Input, deltas, r, c, ch);
                        }
                    }
                }
            }






        }
    }

    class ConvolutionalNeuralNetwork {
    }
}
