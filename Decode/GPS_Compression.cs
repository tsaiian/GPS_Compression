using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Numerics;

namespace Decode
{
    class GPS_Compression
    {
        private List<string> regionNameInfo = new List<string>();
        private Dictionary<int, string> huffmanCodeWordTable = new Dictionary<int, string>();
        private List<List<double>> all_x = new List<List<double>>();
        private List<List<double>> all_y = new List<List<double>>();

        #region Constructor and data prepare
        public GPS_Compression()
        {
            ReadData();
        }

        private void ReadData()
        {
            Console.WriteLine("[Loading map]");
            List<double> x = new List<double>();
            List<double> y = new List<double>();

            StreamReader sr = new StreamReader("data\\boundary.txt");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (line.StartsWith("121.") || line.StartsWith("120.") || line.StartsWith("119."))
                {
                    y.Add(Convert.ToDouble(line.Substring(line.IndexOf(" ") + 1)));
                    x.Add(Convert.ToDouble(line.Substring(0, line.IndexOf(" "))));
                }
                else if (line.StartsWith("PEN(1,2,7237230)"))
                {
                    all_x.Add(x);
                    all_y.Add(y);

                    x = new List<double>();
                    y = new List<double>();
                }
            }
            sr.Close();

            sr = new StreamReader("data\\TWN_VILLAGE.mid");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                regionNameInfo.Add(line);
            }
            sr.Close();

            Console.WriteLine("[Loading codeword]");
            sr = new StreamReader("data\\huffmanCodeTable.txt");
            int i = 0;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string strCodeword = line.Substring(line.IndexOf('\t') + 1);

                huffmanCodeWordTable.Add(i++, strCodeword);
            }
        }
        #endregion

        public Tuple<double, double> Decode(BitArray codeword, double old_x, double old_y)
        {
            List<bool> temp = new List<bool>();
            for (int i = 1; i < codeword.Length; i++)
                temp.Add(codeword[i]);


            if (codeword.Length > 0 && codeword[0] == true)
                return ReferencePositionDecode(new BitArray(temp.ToArray()), old_x, old_y);
            else
                return AbsolutePositionDecode(new BitArray(temp.ToArray()));
        }

        #region Reference position related function

        private Tuple<double, double> ReferencePositionDecode(BitArray codeword, double old_x, double old_y)
        {
            string str = "";
            foreach(bool b in codeword)
                str += (b ? "1" : "0");

            if (str.Equals("111"))
                return new Tuple<double, double>(old_x, old_y);
            else if (str.Equals("0111"))
                return new Tuple<double, double>(old_x, old_y + 0.00001);
            else if (!str.EndsWith("0111"))
                return null;

            Tuple<int, int> diff = InverseCD((int)DecodeInt(str) + 2);

            double resultX = old_x + ((double)diff.Item1 * 0.00001);
            double resultY = old_y + ((double)diff.Item2 * 0.00001);

            return new Tuple<double, double>(resultX, resultY);
        }

        private long DecodeInt(string s)
        {
            //remove last 0111
            string str = "";
            for (int i = 0; i < s.Length - 4; i++)
                str += s[i];

            long n = Convert.ToInt64(str, 2);
            return n - HoleCount(n);
        }

        private BigInteger Factorial(int n)
        {
            BigInteger r = 1;
            for (int i = 1; i <= n; i++)
                r *= i;

            return r;
        }

        private long HoleCount(long num)
        {
            string bin = Convert.ToString(num, 2);
            int n = bin.Length;

            long result = 0;
            bool threeOneAlready = false;
            for (int i = 0; i < n; i++)
            {
                if (bin[i] == '1')
                {
                    //1[0]+
                    string oneZeros = "1";
                    for (int j = 0; j < n - i - 1; j++)
                        oneZeros += "0";

                    if (!threeOneAlready)
                        result += OneHoleCount(oneZeros);
                    else
                        result += (int)Math.Pow(2, n - i -1);

                    if (i >= 2 && bin[i - 1] == '1' && bin[i - 2] == '1')
                    {
                        if (!threeOneAlready)
                        {
                            threeOneAlready = true;
                            result++;
                        }
                    }
                }
            }
            return result;
        }

        private long OneHoleCount(string bin)
        {//bin must be "1", "10", "100", "1000" ...
            int n = bin.Length - 1;

            long totalCount = 0;
            for (int i = 3; i <= n; i++)
            {
                int zeroCount = n - i;
                int oneCount = i;

                //all possible combination
                long total = (long)(Factorial(zeroCount + oneCount) / Factorial(zeroCount) / Factorial(oneCount));

                //divide to "11" and "1", and return the number of "11" and "1", ex. 11111 => (0, 5), (1, 3), (2, 1)
                List<Tuple<int, int>> r = Grouping(oneCount);

                long back = 0;
                foreach (Tuple<int, int> t in r)
                    back += (int)(Combination(zeroCount + 1, t.Item1 + t.Item2) * Factorial(t.Item1 + t.Item2) / Factorial(t.Item1) / Factorial(t.Item2));

                totalCount += total - back;
            }
            return totalCount;
        }

        private int Combination(int a, int b)
        {
            if (b > a)
                return 0;

            int r = (int)(Factorial(a) / Factorial(b) / Factorial(a - b));
            return r;
        }

        List<Tuple<int, int>> Grouping(int num)
        {//divide to "11" and "1", and return the number of "11" and "1", ex. 11111 => (0, 5), (1, 3), (2, 1)
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            for (int i = 0; i < num / 2 + 1; i++)
                result.Add(new Tuple<int, int>(i, num - i * 2));
            
            return result;
        }

        private Tuple<int, int> InverseCD(int difference)
        {
             int edge = 0;
             for (int i = 1; ; i += 2)
                 if (i * i > difference)
                 {
                     edge = i;
                     break;
                 }
             if (difference == 0)
                 return new Tuple<int, int>(0, 0);
             int level = (edge - 1) / 2;
             int init = (int)(Math.Pow(edge - 2, 2));
             List<int> repeat = new List<int>() { 4 };
             for (int i = level - 1; i > 0; i--)
                 repeat.Add(8);
             repeat.Add(-3);
             for (int i = level - 1; i > 0; i--)
                 repeat.Add(-8);
             int x = 0;
             int y = 0;
             int index = 0;
             if (init == difference)
                 return new Tuple<int, int>(0, level);

             y = level;
             for (x += 1; x <= edge / 2; x++)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             x -= 1;
             for (y -= 1; y >= 0; y--)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             y = 0;
             x = level;
             index = 0;
             for (y = -1; y >= -edge / 2; y--)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             y += 1;
             for (x -= 1; x >= 0; x--)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }

             x = 0;
             y = -level;
             index = 0;
             for (x = -1; x >= -edge / 2; x--)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             x += 1;
             for (y += 1; y <= 0; y++)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             y -= 1;

             x = -level;
             index = 0;
             repeat[repeat.IndexOf(-3)] = -7;
             for (y += 1; y <= edge / 2; y++)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             y -= 1;
             for (x += 1; x <= edge / 2; x++)
             {
                 init += repeat[index++];
                 if (init == difference)
                     return new Tuple<int, int>(x, y);
             }
             throw new Exception("InverseCD error");
        }

        #endregion

        #region Absolute position related function

        private Tuple<double, double> AbsolutePositionDecode(BitArray codeword)
        {
            //decode first part
            int regionID = 0;
            bool huffmanDecodeSuccessful = false;
            string temp = "";
            foreach (bool b in codeword)
            {
                temp += (b ? "1" : "0");

                if (huffmanCodeWordTable.ContainsValue(temp))
                {
                    regionID = huffmanCodeWordTable.FirstOrDefault(x => x.Value == temp).Key;
                    huffmanDecodeSuccessful = true;
                    break;
                }
            }

            if (!huffmanDecodeSuccessful)
                return null;

            //decode remain part
            string secondPart = "";
            for (int i = temp.Length; i + 6 < codeword.Length; i++)
                secondPart += (codeword[i] ? "1" : "0");

            string thirdPart = "";
            for (int i = codeword.Length - 6; i < codeword.Length; i++)
                thirdPart += (codeword[i] ? "1" : "0");

            try
            {
                Tuple<double, double, int> result = DecodeRemainPart(regionID, Convert.ToInt32(secondPart, 2), Convert.ToInt32(thirdPart, 2), all_x[regionID], all_y[regionID]);
                if (Convert.ToString(result.Item3, 2).Length + 6 != codeword.Length - temp.Length)
                    return null;
                else
                    return new Tuple<double, double>(result.Item1, result.Item2);
                
            }
            catch
            {
                return null;
            }
        }

        private Tuple<double, double, int> DecodeRemainPart(int regionID, int NumInRegion, int detailNum, List<double> lx, List<double> ly)
        {
            double minX = findMin(lx);
            double maxX = findMax(lx);
            double minY = findMin(ly);
            double maxY = findMax(ly);

            int inRegionNum = -1, inRegionTotalNum = 0;;
            bool found = false;
            double resultX = 0, resultY = 0;

            double _y = Math.Ceiling(minY * 100000) / 100000;
            for (; _y < maxY; _y += 0.00008)
            {
                List<double> points = LineCrossNum(_y, lx, ly, minX, maxX);
                bool inRegion = false;

                double _x = Math.Ceiling(minX * 100000) / 100000;
                for (; _x < maxX; _x += 0.00008)
                {
                    int matchCount = 0;
                    foreach (double point in points)
                        if (_x >= point && _x - 0.00008 <= point)
                            matchCount++;

                    if (matchCount % 2 == 1)
                        inRegion = inRegion ? false : true;


                    if (inRegion)
                    {
                        inRegionTotalNum++;

                        if (!found)
                            inRegionNum++;
                    }


                    if (inRegion && inRegionNum == NumInRegion && !found)
                    {
                        found = true;

                        double deltaY = (int)((double)detailNum / 8) * 0.00001;
                        double deltaX = (int)((double)detailNum % 8) * 0.00001;

                        resultX = Math.Round(_x + deltaX, 5);
                        resultY = Math.Round(_y + deltaY, 5);
                    }
                }
            }
            if (!found)
                return null;
            else
                return new Tuple<double, double, int>(resultX, resultY, inRegionTotalNum);
        }

        private List<double> LineCrossNum(double y, List<double> lx, List<double> ly, double minX, double maxX)
        {
            int corssLineCount = 0;
            List<double> points = new List<double>();
            for (int i = 0; i < lx.Count; i++)
            {
                double fp;
                if (i != lx.Count - 1)
                    fp = fixPoint(lx[i], ly[i], lx[i + 1], ly[i + 1], y, minX, maxX);
                else
                    fp = fixPoint(lx[i], ly[i], lx[0], ly[0], y, minX, maxX);

                if (fp != 0)
                {
                    corssLineCount++;
                    points.Add(fp);
                }
            }

            if (corssLineCount % 2 != 0)
            {
                Console.WriteLine("odd number error.");
                throw new Exception("Odd number error.");
            }
            return points;
        }

        private double fixPoint(double x1, double y1, double x2, double y2, double y, double minX, double maxX)
        {
            double x;
            if (y1 != y2 && ((y1 <= y && y2 >= y) || (y1 >= y && y2 <= y)))
            {
                x = (x1 - x2) * (y - y1) / (y1 - y2) + x1;
                if (x >= minX && x <= maxX)
                    return x;
                else
                    return 0;
            }
            else
                return 0;
        }

        private double findMax(List<double> number)
        {
            double res = 0;
            foreach (double d in number)
                if (d > res)
                    res = d;

            return res;
        }

        private double findMin(List<double> number)
        {
            double res = 99999999;
            foreach (double d in number)
                if (d < res)
                    res = d;

            return res;
        }
        #endregion
    }
}
