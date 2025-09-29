using Accord.Math.Differentiation;
using Recorder.MFCC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Recorder
{
    public class DTWFunctions
    {
        public static double DTW_with_pruning(Sequence input, Sequence template, int width)
        {
            int input_len = input.Frames.Length;
            int template_len = template.Frames.Length;
            width = Math.Max(width, 2 * Math.Abs(input_len - template_len));

            int start_index = Math.Max(1, 1 - width / 2);
            int end_index = Math.Min(template_len, 1 + width / 2);
            int band_len = end_index - start_index + 1;

            double[] prevRow = new double[band_len];
            for (int i = 0; i < band_len; i++)
            {
                prevRow[i] = double.PositiveInfinity;
            }


            if (start_index <= 1 && 1 <= end_index)
            {
                int band_idx = 1 - start_index;
                double distance = 0;
                for (int f = 0; f < 13; f++)
                {
                    double difference = input.Frames[0].Features[f] - template.Frames[0].Features[f];
                    distance += difference * difference;
                }
                prevRow[band_idx] = Math.Sqrt(distance);
            }


            for (int i = 2; i <= input_len; i++)
            {
                start_index = Math.Max(1, i - width / 2);
                end_index = Math.Min(template_len, i + width / 2);
                band_len = end_index - start_index + 1;

                double[] currRow = new double[band_len];
                for (int k = 0; k < band_len; k++)
                {
                    currRow[k] = double.PositiveInfinity;
                }

                int prev_start_index = Math.Max(1, i - 1 - width / 2);
                int prev_end_index = Math.Min(template_len, i - 1 + width / 2);

                for (int j = start_index; j <= end_index; j++)
                {
                    int band_idx = j - start_index;

                    double distance = 0;
                    for (int f = 0; f < 13; f++)
                    {
                        double diffeence = input.Frames[i - 1].Features[f] - template.Frames[j - 1].Features[f];
                        distance += diffeence * diffeence;
                    }
                    distance = Math.Sqrt(distance);

                    double min_cost = double.PositiveInfinity;

                    if (j - 1 >= prev_start_index && j - 1 <= prev_end_index)
                    {
                        int index = j - 1 - prev_start_index;
                        if (index >= 0 && index < prevRow.Length)
                            min_cost = Math.Min(min_cost, prevRow[index]);
                    }


                    if (j >= prev_start_index && j <= prev_end_index)
                    {
                        int index = j - prev_start_index;
                        if (index >= 0 && index < prevRow.Length)
                            min_cost = Math.Min(min_cost, prevRow[index]);
                    }

                    if (j - 2 >= prev_start_index && j - 2 <= prev_end_index)
                    {
                        int index = j - 2 - prev_start_index;
                        if (index >= 0 && index < prevRow.Length)
                            min_cost = Math.Min(min_cost, prevRow[index]);
                    }

                    currRow[band_idx] = distance + min_cost;
                }

                prevRow = currRow;
            }

            int final_start_index = Math.Max(1, input_len - width / 2);
            int final_end_index = Math.Min(template_len, input_len + width / 2);

            if (template_len >= final_start_index && template_len <= final_end_index)
            {
                int final_index = template_len - final_start_index;
                return prevRow[final_index];
            }
            else
            {
                return double.PositiveInfinity;
            }
        }
        public static double DTW_WithoutPrunning(Sequence input, Sequence compare)
        {
            double minResult = double.PositiveInfinity;
            int rows = input.Frames.Length + 1;
            int col = compare.Frames.Length + 1;
            double[,] costMatrix = new double[2, col];
            for (int j = 1; j < col; j++)
            {
                costMatrix[0, j] = double.PositiveInfinity;
            }
            costMatrix[0, 0] = 0;
            costMatrix[1, 0] = double.PositiveInfinity;
            double result = 0.0;
            double diffrence = 0;
            double total = 0.0;
            for (int i = 1; i < rows; i++)
            {
                for (int j = 1; j < col; j++)
                {
                     result = 0.0;
                     diffrence = 0;
                     total = 0.0;
                    for (int l = 0; l < 13; l++)
                    {
                        diffrence = input.Frames[i-1].Features[l] - compare.Frames[j - 1].Features[l];
                        diffrence *= diffrence;
                        total += diffrence;
                    }
                    result = Math.Sqrt(total);
                    double Shrink = double.PositiveInfinity;

                    if (j - 2 >= 0)
                    {
                        Shrink = costMatrix[0, j - 2]; 
                    }
                    costMatrix[1, j] = result + Math.Min(Math.Min(costMatrix[0, j - 1], costMatrix[0, j]), Shrink);
                }
                if(i != rows - 1)
                {
                    for (int j = 0; j < col; j++)
                    {
                        costMatrix[0, j] = costMatrix[1, j];
                        costMatrix[1, j] = double.PositiveInfinity;
                    }
                }
            }
            //for (int j = 0; j < col; j++)
            //{
            //    if (costMatrix[0, j] < minResult)
            //        minResult = costMatrix[0, j];
            //}
            minResult = costMatrix[1, col - 1];
            return minResult;
        }

        public static double DTW_WithBeamSearch(Sequence input, Sequence compare, double width)
        {
            //width = Math.Max(width,2* Math.Abs(input.Frames.Length-compare.Frames.Length));
             width = 1.5* Math.Max(width,Math.Max(input.Frames.Count(), compare.Frames.Count()));
            double Best = double.PositiveInfinity;
            Stopwatch stopwatch = Stopwatch.StartNew();
            double minResult = double.PositiveInfinity;
            int rows = input.Frames.Length + 1;
            int col = compare.Frames.Length + 1;
            double[,] costMatrix = new double[2, col];
            for (int j = 1; j < col; j++)
            {
                costMatrix[0, j] = double.PositiveInfinity;
            }
            costMatrix[0, 0] = 0;
            costMatrix[1, 0] = double.PositiveInfinity;
            for (int i = 1; i < rows; i++)
            {
                Best = double.PositiveInfinity;
                for (int j = 1; j < col; j++)
                {

                    double result = 0.0;
                    double diffrence = 0;
                    double total = 0.0;
                    double Shrink = double.PositiveInfinity;

                    if (j - 2 >= 0)
                    {
                        Shrink = costMatrix[0, j - 2];
                    }
                    if (costMatrix[0, j] == double.PositiveInfinity && costMatrix[0, j - 1] == double.PositiveInfinity && Shrink == double.PositiveInfinity)
                    {
                        costMatrix[1, j] = double.PositiveInfinity;
                        continue;
                    }
                    for (int l = 0; l < 13; l++)
                    {
                        diffrence = input.Frames[i - 1].Features[l] - compare.Frames[j - 1].Features[l];
                        diffrence *= diffrence;
                        total += diffrence;
                    }
                    result = Math.Sqrt(total);

                    costMatrix[1, j] = result + Math.Min(Math.Min(costMatrix[0, j - 1], costMatrix[0, j]), Shrink);
                    if (costMatrix[1, j] < Best)
                        Best = costMatrix[1, j];
                }
                double threshold = Best + width;
                for (int j = 1; j < col; j++)
                {
                    if (costMatrix[1, j] > threshold)
                        costMatrix[1, j] = double.PositiveInfinity;
                }
                if (i != rows - 1)
                {
                    for (int j = 0; j < col; j++)
                    {
                        costMatrix[0, j] = costMatrix[1, j];
                        costMatrix[1, j] = double.PositiveInfinity;
                    }
                }
            }
            minResult = costMatrix[1, col - 1];
            return costMatrix[1, col - 1];
        }
        //    public static  double Beam_Search(Sequence input, List<MFCCFrame> templateFrame, double beamWidth)
        //    {
        //        List<MFCCFrame> inputFrame = input.Frames.ToList();
        //        int N = inputFrame.Count;
        //        int M = templateFrame.Count;

        //        double[,] D = new double[N, M];
        //        double INF = double.PositiveInfinity;

        //        //first cell
        //        D[0, 0] = EuclideanDistance(inputFrame[0], templateFrame[0]);

        //        for (int j = 1; j < M; j++)
        //            D[0, j] = D[0, j - 1] + EuclideanDistance(inputFrame[0], templateFrame[j]);  // Fill first row

        //        for (int i = 1; i < N; i++)
        //            D[i, 0] = D[i - 1, 0] + EuclideanDistance(inputFrame[i], templateFrame[0]);  // first column

        //        for (int i = 1; i < N; i++)
        //        {
        //            double minCost = INF;

        //            for (int j = 1; j < M; j++)
        //            {
        //                double dist = EuclideanDistance(inputFrame[i], templateFrame[j]);
        //                double cost = Math.Min(Math.Min(D[i - 1, j], D[i - 1, j - 1]), D[i,j-1]) + dist;

        //                D[i, j] = cost;
        //                if (cost < minCost)
        //                    minCost = cost;
        //            }

        //            double threshold = minCost + beamWidth;
        //            for (int j = 1; j < M; j++)
        //            {
        //                if (D[i, j] > threshold)
        //                    D[i, j] = INF;
        //            }
        //        }

        //        double finalCost = INF;
        //        for (int j = 0; j < M; j++)
        //        {
        //            if (D[N - 1, j] < finalCost)
        //                finalCost = D[N - 1, j];
        //        }

        //        return finalCost;
        //    }

        //    private static double EuclideanDistance(MFCCFrame a, MFCCFrame b)
        //    {
        //        double sum = 0.0;
        //        for (int i = 0; i < 13; i++)
        //        {
        //            double diff = a.Features[i] - b.Features[i];
        //            sum += diff * diff;
        //        }
        //        return Math.Sqrt(sum);
        //    }
    }
}
