using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Numerics;

namespace Encode
{
    class GPS_Compression
    {
        private List<string> regionNameInfo = new List<string>();
        private List<string> regionPeopleInfo = new List<string>();
        private Dictionary<int, Tuple<int, int>> intMap = new Dictionary<int, Tuple<int, int>>();
        private List<List<double>> all_x = new List<List<double>>();
        private List<List<double>> all_y = new List<List<double>>();

        #region Constructor and data prepare
        public GPS_Compression()
        {
            ReadData();
            //buildIntMap();
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

            sr = new StreamReader("data\\IntMap.txt");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] tokens = line.Split('\t');

                intMap.Add(Convert.ToInt16(tokens[0]), new Tuple<int, int>(Convert.ToInt32(tokens[1]), Convert.ToInt32(tokens[2])));
            }
            sr.Close();
        }
        #endregion

        public BitArray Encode(double input_x, double input_y, double old_x = 0, double old_y = 0)
        {
            if (old_x == 0 && old_y == 0)
                return AbsolutePositionEncode(input_x, input_y);
            if(Math.Abs(input_x - old_x) > 0.2 || Math.Abs(input_y - old_y) > 0.2)
                return AbsolutePositionEncode(input_x, input_y);

            BitArray refCodeword = ReferencePositionEncode(input_x, input_y, old_x, old_y);

            if (refCodeword.Length < 32)
                return refCodeword;
                
            BitArray absCodeword = AbsolutePositionEncode(input_x, input_y);

            if (refCodeword.Length < absCodeword.Length)
                return refCodeword;
            else
                return absCodeword;
        }

        #region Reference position related function

        private BitArray ReferencePositionEncode(double input_x, double input_y, double old_x, double old_y)
        {
            int deltaX = (int)Math.Round((input_x - old_x) * 100000, 0);
            int deltaY = (int)Math.Round((input_y - old_y) * 100000, 0);

            int refInt = CircleDifference(deltaX, deltaY) - 2;
            if(refInt == -2)
                return new BitArray(new bool[] { false });
            else if(refInt == -1)
                return new BitArray(new bool[] { true });

            string binary = encodeInt(refInt);

            List<bool> bits = new List<bool>();

            //first bit -> ref
            bits.Add(true);
            
            foreach (char s in binary)
                bits.Add((s == '0') ? false : true);

            return new BitArray(bits.ToArray());
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
                result = inThisRegion(id, input_x, input_y);
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
                throw new Exception("Not in Taiwan!");
            }

            string zero = "";
            int zeroCount = 6 - Convert.ToString((int)result["detailBlockNum"], 2).Length;
            for (int i = 0; i < zeroCount; i++)
                zero += "0";

            string thirdBinCode = zero + Convert.ToString((int)result["detailBlockNum"], 2);


            //Console.WriteLine("Region ID: " + regionID);
            //Console.WriteLine("Region Info: " + regionNameInfo[regionID]);
            //Console.WriteLine("People Info: " + getPeopleInfo(regionID));
            //Console.ReadKey();

            int n = intMap[regionID].Item1 + (int)result["inRegionNum"];
            string intBin = encodeInt(n);
            
            List<bool> bits = new List<bool>();

            //first bits -> abs
            bits.Add(false);

            foreach (char s in (thirdBinCode + intBin))
                bits.Add((s == '0') ? false : true);

            return new BitArray(bits.ToArray());
        }

        public string getPeopleInfo(int regionID)
        {
            string nameInfo = regionNameInfo[regionID];

            string s1 = nameInfo.Split(new char[] { ',' })[3].Replace("高雄縣", "高雄市").Replace("台中縣", "台中市").Replace("台南縣", "台南市");
            string s2 = nameInfo.Split(new char[] { ',' })[4].Replace("臺", "台");
            string s3 = nameInfo.Split(new char[] { ',' })[5];

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

        public Dictionary<string, double> inThisRegion(int id, double input_x, double input_y)
        {
            List<double> lx = all_x[id];
            List<double> ly = all_y[id];

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
                _y = Math.Round(_y, 5);
                List<double> points = LineCrossNum(_y, lx, ly, minX, maxX);

                //sw.Write(points.Count + " ");

                bool inRegion = false;

                double _x = Math.Ceiling(minX * 100000) / 100000;
                for (; _x < maxX; _x += 0.00008)
                {
                    _x = Math.Round(_x, 5);
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

                    if (input_x < Math.Round(_x + 0.00008, 5) && input_x >= Math.Round(_x, 5) && input_y < Math.Round(_y + 0.00008, 5) && input_y >= Math.Round(_y, 5) && inRegion)
                    {
                        found = true;
                        isInRegion = true;

                        int detailBlockNum = (int)Math.Round((Math.Round(input_y - _y, 5) * 100000) * 8 + Math.Round(input_x -_x, 5) * 100000, 5);
                        result.Add("detailBlockNum", detailBlockNum);
                    }
                }
            }

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

        private string encodeInt(int i)
        {
            i += 2;

            string result = "";
            if (i % 2 == 0)
                result = Convert.ToString(i / 2, 2);
            else
            {
                string s = Convert.ToString(Math.Abs(i / 2), 2);
                for (int k = 0; k < s.Length; k++)
                {
                    if (s[k] == '0')
                        result += '1';
                    else
                        result += '0';
                }
            }

            return result;

        }

        private void buildIntMap()
        {
            List<Tuple<int, int, double>> allRegionInfo = new List<Tuple<int, int, double>>();

            for (int i = 0; i < regionNameInfo.Count; i++)
            {
                Console.WriteLine(i);
                string peopleInfo = getPeopleInfo(i);

                int peopleCount = 0;
                int totalPointInRegion = (int)inThisRegion(i, 0, 0)["totalPointInRegion"];
                double density = 0;
                if (peopleInfo.Equals("no data") || totalPointInRegion <= 500)
                {
                    density = 0.03;
                }
                else
                {
                    peopleCount = Convert.ToInt32(peopleInfo.Split(new char[] { ',' })[7]);
                    density = ((double)peopleCount / totalPointInRegion);
                }

                Console.WriteLine("D:" + density + "\t" + peopleInfo);

                allRegionInfo.Add(new Tuple<int, int, double>(i, totalPointInRegion, density));
            }

            allRegionInfo.Sort(
                delegate(Tuple<int, int, double> firstPair, Tuple<int, int, double> nextPair)
                {
                    return firstPair.Item3.CompareTo(nextPair.Item3) * (-1);
                }
            );

            StreamWriter sw = new StreamWriter("IntMap.txt");

            int num = 0;
            for (int i = 0; i < regionNameInfo.Count; i++)
            {
                sw.WriteLine(allRegionInfo[i].Item1 + "\t" + num + "\t" + (num + allRegionInfo[i].Item2 - 1) + "\t" + allRegionInfo[i].Item3);
                sw.Flush();

                num += allRegionInfo[i].Item2;
            }
            sw.Close();
            Console.WriteLine("Build Map Finish!");
            Console.ReadKey();

        }
    }
}
