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
            Tuple<double, double> input = new Tuple<double, double>(22.64331, 120.30272);
            Tuple<double, double> reference = new Tuple<double, double>(22.74452, 120.30210);

            GPS_Compression GPSC = new GPS_Compression();

            //encode
            BitArray codeword = GPSC.Encode(input.Item2, input.Item1, reference.Item2, reference.Item1);

            //decode
            Tuple<double, double> result = GPSC.Decode(codeword, reference.Item2, reference.Item1);
            if (result != null)
                Console.WriteLine(result.Item2 + "\t" + result.Item1);
            else
                Console.WriteLine("error codeword");
            
            Console.ReadKey();
        }
    }
}
