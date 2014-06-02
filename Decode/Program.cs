using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace Decode
{
    class Program
    {
        static void Main(string[] args)
        {
            GPS_Compression GPSC = new GPS_Compression();

            string[] inputs = Directory.GetFiles(@".\input", "*.out");
            foreach (string file in inputs)
            {
                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(file + ".txt");
                
                string line = sr.ReadLine();

                List<bool> bList = new List<bool>();
                double preY = 0, preX = 0;
                for (int i = 0; i < line.Length; i++ )
                {
                    //Console.WriteLine("loop" + (i + 1));
                    bList.Add((line[i] == '0') ? false : true);
                    BitArray codeword = new BitArray(bList.ToArray());

                    //decode
                    Tuple<double, double> result = GPSC.Decode(codeword, preX, preY);
                    if (result != null)
                    {
                        sw.WriteLine(result.Item2 + " " + result.Item1);
                        preX = result.Item1;
                        preY = result.Item2;
                        bList.Clear();
                    }
                }
                Console.ReadKey();
                sr.Close();
                sw.Close();
            }
        }
    }
}
