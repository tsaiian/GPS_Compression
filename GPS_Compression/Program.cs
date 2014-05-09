﻿using System;
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
            double x = 121.33799;
            double y = 24.14939;


            GPS_Compression GPSC = new GPS_Compression();

            BitArray codeword = GPSC.Encode(x, y);

            Tuple<double, double> result = GPSC.Decode(codeword);
            if (result != null)
                Console.WriteLine(result.Item1 + "\t" + result.Item2);
            else
                Console.WriteLine("error codeword");
            
            Console.ReadKey();
        }
    }
}
