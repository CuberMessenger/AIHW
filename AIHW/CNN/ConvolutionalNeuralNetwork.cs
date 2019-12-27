using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHW.CNN {
    class ConvolutionalLayer {
        internal int NumOfKernal { get; set; }
        internal (int, int, int) KernalShape { get; set; }
        internal (int, int, int) InputShape { get; set; }
        internal (int, int, int) OutputShape { get; set; }
        internal (int, int) PaddingShape { get; set; }
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
            PaddingShape = ((KernalShape.Item2 - 1) / 2, (KernalShape.Item3 - 1) / 2);// 3x3 kernal -> (1, 1)
            InputShape = inputShape;
            OutputShape = (NumOfKernal, InputShape.Item2 - 2 * PaddingShape.Item1, InputShape.Item3 - 2 * PaddingShape.Item2);

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

            ActivationFunction = ReLu;
            ActivationDerivative = ReLuDerivative;
        }

        internal float Convolution(float[,,] input, float[,,] kernal, int startRow, int startColumn, int channel) {
            float answer = 0f;
            for (int r = startRow; r < startRow + KernalShape.Item1; r++) {
                for (int c = startColumn; c < startColumn + KernalShape.Item2; c++) {
                    answer += input[channel, r, c] * kernal[channel, r - startRow, c - startColumn];
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
            var paddedInput = new float[inputShape.Item2 + 2 * paddingShape.Item1, inputShape.Item3 + 2 * paddingShape.Item2];
            for (int r = 0; r < paddedInput.GetLength(1); r++) {
                for (int c = 0; c < paddedInput.GetLength(2); c++) {
                    if (r == 0 || c == 0 || r == paddedInput.GetLength(1) - 1 || c == paddedInput.GetLength(2) - 1) {
                        paddedInput[r, c] = 0f;
                    }
                    else {
                        paddedInput[r, c] = input[indexOfInput, r + paddingShape.Item1, c + paddingShape.Item2];
                    }
                }
            }

            for (int ch = 0; ch < InputShape.Item1; ch++) {
                for (int r = 0; r < InputShape.Item2; r++) {
                    for (int c = 0; c < InputShape.Item3; c++) {
                        for (int kr = 0; kr < kernalShape.Item2; kr++) {
                            for (int kc = 0; kc < kernalShape.Item3; kc++) {
                                answer[ch, r, c] += paddedInput[r + kr, c + kc] * kernal[indexOfInput, kr, kc];
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
                            FeatureMap[k, r, c] = Convolution(Input, Kernals[k], r, c, ch) + Biases[k];
                        }
                    }
                }
            }
            return FeatureMap;
        }

        internal void Backward(float[,,] deltaFeatureMap) {
            //dY is gradient of current layer output [Channel(NumOfKernal), Height, Width]
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
                            DeltaKernals[k][ch, r, c] = Convolution(Input, DeltaFeatureMap, r, c, ch);
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
    }
}
