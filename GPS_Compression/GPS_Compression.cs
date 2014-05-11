using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Numerics;

namespace prog
{
    class GPS_Compression
    {
        private List<string> regionNameInfo = new List<string>();
        private List<string> regionPeopleInfo = new List<string>();
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

            sr = new StreamReader("data\\U01VI_102Y12M_TW.csv");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (line.StartsWith("\""))
                    regionPeopleInfo.Add(line);
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

        public BitArray Encode(double input_x, double input_y, double old_x = 0, double old_y = 0)
        {
            if (old_x == 0 && old_y == 0)
                return AbsolutePositionEncode(input_x, input_y);
            if(Math.Abs(input_x - old_x) > 0.2 || Math.Abs(input_y - old_y) > 0.2)
                return AbsolutePositionEncode(input_x, input_y);

            BitArray refCodeword = ReferencePositionEncode(input_x, input_y, old_x, old_y);
            BitArray absCodeword = AbsolutePositionEncode(input_x, input_y);

            Console.WriteLine(refCodeword.Length + "\t" + absCodeword.Length);


            if (refCodeword.Length < absCodeword.Length)
                return refCodeword;
            else
                return absCodeword;
        }

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

        private BitArray ReferencePositionEncode(double input_x, double input_y, double old_x, double old_y)
        {
            int deltaX = (int)(Math.Round((input_x - old_x), 5) * 100000);
            int deltaY = (int)(Math.Round((input_y - old_y), 5) * 100000);

            int refInt = CircleDifference(deltaX, deltaY);
            string binary = EncodeInt(refInt);

            List<bool> bits = new List<bool>();

            //first bit -> ref
            bits.Add(true);
            
            foreach (char s in binary)
                bits.Add((s == '0') ? false : true);

            Console.WriteLine("\n--FIRST BIT--------------\n1");
            Console.WriteLine("refInt:" + refInt);
            Console.WriteLine("EncodeInt:" + binary);
            Console.WriteLine("\n--TOTAL--------------\n1");
            Console.WriteLine("codeword:" + "1" + binary);
            Console.WriteLine("length:" + ("1" + binary).Length + "\n");

            return new BitArray(bits.ToArray());
        }

        private Tuple<double, double> ReferencePositionDecode(BitArray codeword, double old_x, double old_y)
        {
            string str = "";
            foreach(bool b in codeword)
                str += (b ? "1" : "0");

            Tuple<int, int> diff = InverseCD((int)DecodeInt(str));

            double resultX = old_x + ((double)diff.Item1 * 0.00001);
            double resultY = old_y + ((double)diff.Item2 * 0.00001);

            return new Tuple<double, double>(resultX, resultY);
        }

        private string EncodeInt(long n)
        {
            long max = 0;
            long min = 0;

            long temp = n;
            while (true)
            {
                long afterF = temp - HoleCount(temp);

                if (afterF == n)
                    return Convert.ToString(temp, 2) + "1111";
                else if (afterF > n && (temp < max || max == 0))
                    max = temp;
                else if (afterF < n && temp > min)
                    min = temp;

                long preTemp = temp;

                if (max == 0)
                    temp *= 2;
                else
                    temp = (long)((min + max) / 2);
                
                if (temp == preTemp || temp < 0)
                    throw new Exception("Overflow exception");
            }
        }

        private long DecodeInt(string s)
        {
            //remove last 111
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

                    if (i >= 3 && bin[i - 1] == '1' && bin[i - 2] == '1' && bin[i - 3] == '1')
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
            for (int i = 4; i <= n; i++)
            {
                int zeroCount = n - i;
                int oneCount = i;

                //all possible combination
                long total = (long)(Factorial(zeroCount + oneCount) / Factorial(zeroCount) / Factorial(oneCount));

                //divide to "11" and "1", and return the number of "11" and "1", ex. 11111 => (0, 5), (1, 3), (2, 1)
                List<Tuple<int, int, int>> r = Grouping(oneCount);

                long back = 0;
                foreach (Tuple<int, int, int> t in r)
                    back += (int)(Combination(zeroCount + 1, t.Item1 + t.Item2 + t.Item3) * Factorial(t.Item1 + t.Item2 + t.Item3) / Factorial(t.Item1) / Factorial(t.Item2) / Factorial(t.Item3));

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

        private List<Tuple<int, int, int>> Grouping(int num)
        {//divide to "11" and "1", and return the number of "11" and "1", ex. 11111 => (0, 5), (1, 3), (2, 1)
            List<Tuple<int, int, int>> result = new List<Tuple<int, int, int>>();
            for (int j = 0; j < num / 3 + 1; j++)
            {
                int num2 = num - j * 3;
                for (int i = 0; i < num2 / 2 + 1; i++)
                    result.Add(new Tuple<int, int, int>(j, i, num2 - i * 2));
            }
            return result;
        }

        private int CircleDifference(int X, int Y)
        {
            int level = Math.Max(Math.Abs(X), Math.Abs(Y));
            int edge = level * 2 + 1;
            if (level == 0)
                return 0;

            int init = (int)(Math.Pow(edge - 2, 2));
            if (X == 0)
            {
                if (Y > 0)
                    return init;
                return init + 2;

            }
            if (Y == 0)
            {
                if (X > 0)
                    return init + 1;
                return init + 3;
            }
            List<int> repeat = new List<int>() { 4 };
            for (int i = level - 1; i > 0; i--)
                repeat.Add(8);
            repeat.Add(-3);
            for (int i = level - 1; i > 0; i--)
                repeat.Add(-8);
            int Quadrant = 0;
            if (X > 0)
            {
                if (Y < 0)
                {
                    Quadrant = 1;
                }
            }
            else
            {
                if (Y > 0)
                {
                    Quadrant = 3;
                    repeat[repeat.IndexOf(-3)] = -7;
                }
                else
                {
                    Quadrant = 2;
                }
            }
            init += Quadrant;
            int index = 0;
            int x = 0;
            int y = 0;
            switch (Quadrant)
            {
                case (0):
                    y = level;
                    for (x += 1; x <= edge / 2; x++)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    x -= 1;
                    for (y -= 1; y > 0; y--)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    break;
                case (1):
                    x = level;
                    for (y = -1; y >= -edge / 2; y--)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    y += 1;
                    for (x -= 1; x > 0; x--)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    break;
                case (2):
                    y = -level;
                    for (x = -1; x >= -edge / 2; x--)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    x += 1;
                    for (y += 1; y <= edge / 2; y++)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    break;
                case (3):
                    x = -level;
                    for (y += 1; y <= edge / 2; y++)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    y -= 1;
                    for (x += 1; x <= edge / 2; x++)
                    {
                        init += repeat[index++];
                        if (x == X && y == Y)
                            return init;
                    }
                    break;
            }
            throw new Exception("CirlceDifference error");
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
        private BitArray AbsolutePositionEncode(double input_x, double input_y)
        {
            List<int> candiateRegion = new List<int>();
            for (int count = 0; count < all_x.Count; count++)
                if (input_x <= findMax(all_x[count]) && input_x >= findMin(all_x[count]))
                    if (input_y <= findMax(all_y[count]) && input_y >= findMin(all_y[count]))
                        candiateRegion.Add(count);

            int regionNum = -1;
            bool find = false;
            int regionID = 0;
            Dictionary<string, double> result = null;
            foreach (int id in candiateRegion)
            {
                result = inThisRegion(id, input_x, input_y, all_x[id], all_y[id]);
                if ((regionNum = (int)result["inRegionNum"]) != -1)
                {
                    regionID = id;
                    find = true;
                    break;
                }
            }

            if (!find)
            {
                Console.WriteLine("not in Taiwan");
                throw new Exception("Not in Taiwan");
                return null;
            }

            Console.WriteLine("Region ID: " + regionID);
            Console.WriteLine("Region Info: " + regionNameInfo[regionID]);
            Console.WriteLine("People Info: " + getPeopleInfo(regionNameInfo[regionID], regionPeopleInfo));

            Console.WriteLine("\n--FIRST BIT--------------\n0");
            Console.WriteLine("\n--FIRST PART--------------");
            Console.WriteLine("first part codeword:" + huffmanCodeWordTable[regionID] + "(" + (huffmanCodeWordTable[regionID]).Length + " bits)");

            Console.WriteLine("\n--SECOND PART-------------");
            Console.WriteLine("Total point count in Region: " + result["totalPointInRegion"]);
            Console.WriteLine("No. x in Region: " + regionNum);

            string zero = "";
            int zeroCount = Convert.ToString((int)result["totalPointInRegion"], 2).Length - Convert.ToString(regionNum, 2).Length;
            for (int i = 0; i < zeroCount; i++)
                zero += "0";

            string secondBinCode = zero + Convert.ToString(regionNum, 2);

            Console.WriteLine("second part codeword:" + secondBinCode + "(" + secondBinCode.Length + " bits)");

            Console.WriteLine("\n--THIRD PART--------------");

            zero = "";
            zeroCount = 6 - Convert.ToString((int)result["detailBlockNum"], 2).Length;
            for (int i = 0; i < zeroCount; i++)
                zero += "0";

            string thirdBinCode = zero + Convert.ToString((int)result["detailBlockNum"], 2);
            Console.WriteLine("third part codeword: " + thirdBinCode + "(" + thirdBinCode.Length + " bits)");


            Console.WriteLine("\n--TOTAL-------------------");
            Console.WriteLine("Codeword: " + "0" + huffmanCodeWordTable[regionID] + secondBinCode + thirdBinCode);
            Console.WriteLine("Length: " + ("0" + huffmanCodeWordTable[regionID] + secondBinCode + thirdBinCode).Length + " bits");

            List<bool> bits = new List<bool>();

            //first bits -> abs
            bits.Add(false);

            foreach (char s in (huffmanCodeWordTable[regionID] + secondBinCode + thirdBinCode))
                bits.Add((s == '0') ? false : true);

            return new BitArray(bits.ToArray());
        }

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

            return DecodeRemainPart(regionID, Convert.ToInt32(secondPart, 2), Convert.ToInt32(thirdPart, 2), all_x[regionID], all_y[regionID]);

        }

        private Tuple<double, double> DecodeRemainPart(int regionID, int NumInRegion, int detailNum, List<double> lx, List<double> ly)
        {
            double minX = findMin(lx);
            double maxX = findMax(lx);
            double minY = findMin(ly);
            double maxY = findMax(ly);

            int inRegionNum = -1;
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
                        inRegionNum++;


                    if (inRegion && inRegionNum == NumInRegion)
                    {
                        double deltaY = (int)((double)detailNum / 6) * 0.00001;
                        double deltaX = (int)((double)detailNum % 6) * 0.00001;

                        return new Tuple<double, double>(Math.Round(_x + deltaX, 5), Math.Round(_y + deltaY, 5));
                    }
                }
            }
            return null;
        }

        static public string getPeopleInfo(string regionNameInfo, List<string> regionPeopleInfo)
        {
            string s1 = regionNameInfo.Split(new char[] { ',' })[3].Replace("高雄縣", "高雄市").Replace("台中縣", "台中市").Replace("台南縣", "台南市");
            string s2 = regionNameInfo.Split(new char[] { ',' })[4].Replace("臺", "台");
            string s3 = regionNameInfo.Split(new char[] { ',' })[5];

            List<string> all_s1 = new List<string>();
            List<string> all_s2 = new List<string>();
            List<string> all_s3 = new List<string>();

            foreach (string s in regionPeopleInfo)
            {
                all_s1.Add(s.Split(new char[] { ',' })[1].Replace("\"", "").Replace("臺", "台").Replace("新北市", "台北縣"));
                all_s2.Add(s.Split(new char[] { ',' })[3].Replace("\"", "").Replace("臺", "台"));
                all_s3.Add(s.Split(new char[] { ',' })[5].Replace("\"", ""));
            }
            bool alreadyMatch = false;
            int result = 0;
            for (int i = 0; i < all_s1.Count; i++)
            {
                if (all_s1[i].Equals(s1) && all_s2[i].Equals(s2) && all_s3[i].Equals(s3))
                {
                    alreadyMatch = true;
                    result = i;
                }
            }
            if (!alreadyMatch)
            {
                for (int i = 0; i < all_s1.Count; i++)
                {
                    if (all_s1[i].Equals(s1) && all_s2[i].Substring(0, all_s2[i].Length - 1).Equals(s2.Substring(0, s2.Length - 1)) && all_s3[i].Substring(0, all_s3[i].Length - 1).Equals(s3.Substring(0, s3.Length - 1)))
                    {
                        alreadyMatch = true;
                        result = i;
                    }
                }
            }

            if (alreadyMatch)
                return regionPeopleInfo[result];
            else
                return "no data";

        }

        private Dictionary<string, double> inThisRegion(int id, double input_x, double input_y, List<double> lx, List<double> ly)
        {
            Dictionary<string, double> result = new Dictionary<string, double>();
            bool isInRegion = false, found = false;

            double minX = findMin(lx);
            double maxX = findMax(lx);
            double minY = findMin(ly);
            double maxY = findMax(ly);


            int inRegionNum = -1, inRegionTotalNum = 0;
            double _y = Math.Ceiling(minY * 100000) / 100000;
            for (; _y < maxY; _y += 0.00008)
            {
                List<double> points = LineCrossNum(_y, lx, ly, minX, maxX);

                //sw.Write(points.Count + " ");

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

                    if (input_x < Math.Round(_x, 5) + 0.00008 && input_x >= Math.Round(_x, 5) && input_y < Math.Round(_y, 5) + 0.00008 && input_y >= Math.Round(_y, 5) && inRegion)
                    {
                        found = true;
                        isInRegion = true;

                        int detailBlockNum = (int)(Math.Round((input_y - Math.Round(_y, 5)), 5) * 100000) * 6 + (int)(Math.Round((input_x - Math.Round(_x, 5)), 5) * 100000);
                        result.Add("detailBlockNum", detailBlockNum);
                    }
                }
                //foreach (double l in points)
                //    sw.Write(l + " ");
                //sw.WriteLine();

            }
            //sw.Close();
            //return -1;
            //return false;

            if (isInRegion)
                result.Add("inRegionNum", inRegionNum);
            else
                result.Add("inRegionNum", -1);

            result.Add("totalPointInRegion", inRegionTotalNum);

            return result;
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
                Console.ReadKey();
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

        private void BuildHuffmanTree()
        {
            Console.WriteLine("[Build HuffmanTree]");
            HuffmanTree huffmanTree = new HuffmanTree();
            huffmanTree.Build(regionNameInfo, regionPeopleInfo);

            StreamWriter sw = new StreamWriter("huffmanCodeTable.txt");
            for (int i = 0; i < regionNameInfo.Count; i++)
            {
                Console.WriteLine(i);
                BitArray encoded = huffmanTree.Encode(regionNameInfo[i]);
                sw.Write(i + "\t");
                foreach (bool bit in encoded)
                {
                    sw.Write((bit ? 1 : 0) + "");
                }
                sw.Write('\n');

            }
            sw.Close();
        }
    }
}
