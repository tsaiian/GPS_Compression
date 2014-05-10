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
            Tuple<double, double> input = new Tuple<double, double>(120.99120, 24.79497);
            Tuple<double, double> reference = new Tuple<double, double>(120.98313, 24.80204);

            GPS_Compression GPSC = new GPS_Compression();

            //encode
            BitArray codeword = GPSC.Encode(input.Item1, input.Item2, reference.Item1, reference.Item2);

            //decode
            Tuple<double, double> result = GPSC.Decode(codeword, reference.Item1, reference.Item2);
            if (result != null)
                Console.WriteLine(result.Item1 + "\t" + result.Item2);
            else
                Console.WriteLine("error codeword");
            
            Console.ReadKey();
        }
    }
}
