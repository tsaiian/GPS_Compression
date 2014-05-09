using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace prog
{
    class GPS_Compression
    {
        private List<string> regionNameInfo = new List<string>();
        private List<string> regionPeopleInfo = new List<string>();
        private Dictionary<int, string> huffmanCodeWordTable = new Dictionary<int, string>();
        private List<List<double>> all_x = new List<List<double>>();
        private List<List<double>> all_y = new List<List<double>>();

        public GPS_Compression()
        {
            ReadData();
        }
        public Tuple<double, double> Decode(BitArray codeword)
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
        public BitArray Encode(double input_x, double input_y)
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
                return null;
            }

            Console.WriteLine("Region ID: " + regionID);
            Console.WriteLine("Region Info: " + regionNameInfo[regionID]);
            Console.WriteLine("People Info: " + getPeopleInfo(regionNameInfo[regionID], regionPeopleInfo));

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
            Console.WriteLine("Codeword: " + huffmanCodeWordTable[regionID] + secondBinCode + thirdBinCode);
            Console.WriteLine("Length: " + (huffmanCodeWordTable[regionID] + secondBinCode + thirdBinCode).Length + " bits");

            List<bool> bits = new List<bool>();
            foreach (char s in (huffmanCodeWordTable[regionID] + secondBinCode + thirdBinCode))
                bits.Add((s == '0') ? false : true);

            return new BitArray(bits.ToArray());
        }
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

                        return  new Tuple<double, double>(Math.Round(_x + deltaX, 5), Math.Round(_y + deltaY, 5));
                    }
                }
            }
            return null;
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

                //List<bool> bits = new List<bool>();
                //foreach (char s in strCodeword)
                //    bits.Add((s == '0') ? false : true);

                huffmanCodeWordTable.Add(i++, strCodeword);
            }
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


    }
}
