using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace prog
{
    class Program
    {
        static void Main(string[] args)
        {
            GPS_Compression GPSC = new GPS_Compression();

            string[] inputs = Directory.GetFiles(@".\input", "*.txt");
            foreach (string file in inputs)
            {
                double preX = 0, preY = 0;
                StreamReader sr = new StreamReader(file);
                StreamWriter sw = new StreamWriter(file + ".mode");
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    
                    double x = Convert.ToDouble(line.Substring(0, line.IndexOf(' ')));
                    double y = Convert.ToDouble(line.Substring(line.IndexOf(' ') + 1));

                    Tuple<double, double> input = new Tuple<double, double>(x, y);
                    Tuple<double, double> reference = new Tuple<double, double>(preX, preY);

                    //encode
                    BitArray codeword = GPSC.Encode(input.Item2, input.Item1, reference.Item2, reference.Item1);

                    foreach (bool b in codeword)
                        sw.Write(b ? "1" : "0");
                    sw.WriteLine();

                    //decode
                    //Tuple<double, double> result = GPSC.Decode(codeword, reference.Item2, reference.Item1);
                    //if (result != null)
                    //    Console.WriteLine(result.Item2 + "\t" + result.Item1);
                    //else
                    //    Console.WriteLine("error codeword");

                    preX = x;
                    preY = y;
                    
                }
                sr.Close();
                sw.Close();
            }
        }
    }
}
