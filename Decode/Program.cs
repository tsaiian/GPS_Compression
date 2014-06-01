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
                double preY = 0, preX = 0;
                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(file + ".txt");
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    List<bool> bList = new List<bool>();
                    foreach (char c in line)
                        bList.Add((c == '0') ? false : true);

                    BitArray codeword = new BitArray(bList.ToArray());

                    //decode
                    Tuple<double, double> result = GPSC.Decode(codeword, preX, preY);
                    if (result != null)
                    {
                        sw.WriteLine(result.Item2 + " " + result.Item1);
                        preX = result.Item1;
                        preY = result.Item2;
                    }
                    else
                        Console.WriteLine("error codeword");

                }
                sr.Close();
                sw.Close();
            }
        }
    }
}
