using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace prog
{
    class HuffmanTree
    {
        private List<Node> nodes = new List<Node>();
        public Node Root { get; set; }
        public Dictionary<string, int> Frequencies = new Dictionary<string, int>();

        public void Build(List<string> regionNameInfo, List<string> regionPeopleInfo)
        {
            for (int i = 0; i < regionNameInfo.Count; i++)
            {
                string peopleInfo = prog.Program.getPeopleInfo(regionNameInfo[i], regionPeopleInfo);

                int peopleCount = 0;
                if (peopleInfo.Equals("no data"))
                    peopleCount = 5000;
                else
                    peopleCount = Convert.ToInt32(peopleInfo.Split(new char[] { ',' })[7]);

                

                Frequencies.Add(regionNameInfo[i], peopleCount);
            }

            foreach (KeyValuePair<string, int> symbol in Frequencies)
            {
                nodes.Add(new Node() { Symbol = symbol.Key, Frequency = symbol.Value });
            }

            while (nodes.Count > 1)
            {
                List<Node> orderedNodes = nodes.OrderBy(node => node.Frequency).ToList<Node>();

                if (orderedNodes.Count >= 2)
                {
                    // Take first two items
                    List<Node> taken = orderedNodes.Take(2).ToList<Node>();

                    // Create a parent node by combining the frequencies
                    Node parent = new Node()
                    {
                        Symbol = "*",
                        Frequency = taken[0].Frequency + taken[1].Frequency,
                        Left = taken[0],
                        Right = taken[1]
                    };

                    nodes.Remove(taken[0]);
                    nodes.Remove(taken[1]);
                    nodes.Add(parent);
                }

                this.Root = nodes.FirstOrDefault();

            }

        }

        public BitArray Encode(string source)
        {
            //List<bool> encodedSource = new List<bool>();

            //for (int i = 0; i < source.Length; i++)
            //{
                List<bool> encodedSymbol = this.Root.Traverse(source, new List<bool>());
                //encodedSource.AddRange(encodedSymbol);
            //}

                BitArray bits = new BitArray(encodedSymbol.ToArray());

            return bits;
        }

        public string Decode(BitArray bits)
        {
            Node current = this.Root;
            string decoded = "";

            foreach (bool bit in bits)
            {
                if (bit)
                {
                    if (current.Right != null)
                    {
                        current = current.Right;
                    }
                }
                else
                {
                    if (current.Left != null)
                    {
                        current = current.Left;
                    }
                }

                if (IsLeaf(current))
                {
                    decoded += current.Symbol;
                    current = this.Root;
                }
            }

            return decoded;
        }

        public bool IsLeaf(Node node)
        {
            return (node.Left == null && node.Right == null);
        }
    }
}
