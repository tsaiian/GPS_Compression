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
            double x = 121.337996;
            double y = 24.149390;

            GPS_Compression GPSC = new GPS_Compression();

            BitArray codeword = GPSC.Encode(x, y);

            List<double> result = GPSC.Decode(codeword);
            if (result != null)
                Console.WriteLine(result[0] + "\t" + result[1]);
            else
                Console.WriteLine("error codeword");
            
            Console.ReadKey();
        }
    }
}
