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
        static private int totalPointInRegion = 0;
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader("data\\boundary.txt");
            double input_x = 120.9665;
            double input_y = 23.9674;

            List<string> regionNameInfo = new List<string>();
            List<string> regionPeopleInfo = new List<string>();
            List<List<double>> all_x = new List<List<double>>();
            List<List<double>> all_y = new List<List<double>>();

            List<string> codeword = new List<string>();

            List<double> x = new List<double>();
            List<double> y = new List<double>();

            int regionNum = -1;

            Console.WriteLine("[Loading map]");
            #region read data
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
                if(line.StartsWith("\""))
                    regionPeopleInfo.Add(line);
            }
            sr.Close();

            #endregion

            Console.WriteLine("[Loading codeword]");
            sr = new StreamReader("data\\huffmanCodeTable.txt");
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                codeword.Add(line.Substring(line.IndexOf('\t') + 1));
            }

            #region Build HuffmanTree
            //Console.WriteLine("[Build HuffmanTree]");
            //HuffmanTree huffmanTree = new HuffmanTree();
            //huffmanTree.Build(regionNameInfo, regionPeopleInfo);

            //StreamWriter sw = new StreamWriter("huffmanCodeTable.txt");
            //for(int i = 0 ; i < regionNameInfo.Count ; i++)
            //{
            //    BitArray encoded = huffmanTree.Encode(regionNameInfo[i]);
            //    sw.Write(i + "\t");
            //    foreach (bool bit in encoded)
            //    {
            //        sw.Write((bit ? 1 : 0) + "");
            //    }
            //    sw.Write('\n');
                
            //}
            //sw.Close();
            #endregion

            Console.WriteLine("[Finish]");
            //debug_full(regionNameInfo, regionPeopleInfo);
            //Console.ReadKey();

            List<int> candiateRegion = new List<int>();

            for (int count = 0; count < all_x.Count; count++)
                if(input_x <= findMax(all_x[count]) && input_x >= findMin(all_x[count]))
                    if (input_y <= findMax(all_y[count]) && input_y >= findMin(all_y[count]))
                        candiateRegion.Add(count);

            bool find = false;
            int regionID = 0;
            foreach (int id in candiateRegion)
            {
                if ((regionNum = inThisRegion(id, input_x, input_y, all_x[id], all_y[id])) != -1)
                {
                    regionID = id;
                    find = true;
                    break;
                }
            }

            if(find)
                Console.WriteLine("ok");
            else
                Console.WriteLine("not found");

            Console.WriteLine("Region ID: " + regionID);
            Console.WriteLine("Region Info: " + regionNameInfo[regionID]);
            Console.WriteLine("People Info: " + getPeopleInfo(regionNameInfo[regionID], regionPeopleInfo));
            Console.WriteLine("Region codeword:" + codeword[regionID] + "(" + codeword[regionID].Length + " bits)\n");

            Console.WriteLine("Total point count in Region: " + totalPointInRegion);
            Console.WriteLine("No. x in Region: " + regionNum);

            string zero = "";
            int zeroCount = Convert.ToString(totalPointInRegion, 2).Length - Convert.ToString(regionNum, 2).Length;
            for (int i = 0; i < zeroCount; i++)
                zero += "0";

            string binCode = zero + Convert.ToString(regionNum, 2);

            Console.WriteLine("(binary): " + binCode + "(" + binCode.Length + " bits)");



            Console.WriteLine("\n\nCodeword: " + codeword[regionID] + binCode);
            Console.WriteLine("Length: " + (codeword[regionID] + binCode).Length + " bits");
           
            
            
            Console.ReadKey();
        }
        static private void debug_full( List<string> regionNameInfo,  List<string> regionPeopleInfo)
        {
            StreamWriter sw = new StreamWriter("bug.txt");
            foreach (string ss in regionNameInfo)
            {
                string s1 = ss.Split(new char[] { ',' })[3].Replace("高雄縣", "高雄市").Replace("台中縣", "台中市").Replace("台南縣", "台南市");
                string s2 = ss.Split(new char[] { ',' })[4].Replace("臺", "台");
                string s3 = ss.Split(new char[] { ',' })[5];

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

                if (!alreadyMatch)
                {
                    Console.WriteLine(ss);
                    sw.WriteLine(ss);
                    sw.Flush();
                    //Console.ReadKey();
                }

            }
            sw.Close();

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
                    if (all_s1[i].Equals(s1) && all_s2[i].Substring(0, all_s2[i].Length - 1).Equals(s2.Substring(0, s2.Length - 1)) && all_s3[i].Substring(0, all_s3[i].Length - 1 ).Equals(s3.Substring(0, s3.Length - 1)))
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
        static private int inThisRegion(int id, double input_x, double input_y, List<double> lx, List<double> ly)
        {
            bool isInRegion = false, found = false;

            double minX = findMin(lx);
            double maxX = findMax(lx);
            double minY = findMin(ly);
            double maxY = findMax(ly);


            int inRegionNum = -1, inRegionTotalNum = 0;
            double _y = Math.Ceiling(minY * 10000) / 10000;
            for (; _y < maxY; _y += 0.0001)
            {
                List<double> points = LineCrossNum(_y, lx, ly, minX, maxX);

                //sw.Write(points.Count + " ");

                bool inRegion = false;

                double _x = Math.Ceiling(minX * 10000) / 10000;
                for (; _x < maxX; _x += 0.0001)
                {

                    int matchCount = 0;
                    foreach (double point in points)
                    {
                        if (_x >= point && _x - 0.0001 <= point)
                        {
                            matchCount++;
                        }
                    }

                    if (matchCount % 2 == 1)
                        inRegion = inRegion ? false : true;

                    if (inRegion)
                    {
                        inRegionTotalNum++;

                        if(!found)
                            inRegionNum++;
                    }

                    if (input_x == Math.Round(_x, 4) && input_y == Math.Round(_y, 4) && inRegion)
                    {
                        found = true;
                        isInRegion = true;
                        //return inRegionNum;
                        //return true;
                        //sw.Write("!" + " ");
                    }
                    else if (input_x == Math.Round(_x, 4) && input_y == Math.Round(_y, 4))
                    {
                        //return -1;
                        //return false;
                        //sw.Write("-" + " ");
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
            {
                totalPointInRegion = inRegionTotalNum;
                return inRegionNum;
            }
            else
                return -1;
        }
        static private List<double> LineCrossNum(double y, List<double> lx, List<double> ly, double minX, double maxX)
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
        static private double fixPoint(double x1, double y1, double x2, double y2, double y, double minX, double maxX)
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
        static private double findMax(List<double> number)
        {
            double res = 0;
            foreach (double d in number)
                if (d > res)
                    res = d;

            return res;
        }
        static private double findMin(List<double> number)
        {
            double res = 99999999;
            foreach (double d in number)
                if (d < res)
                    res = d;

            return res;
        }

    }
}
