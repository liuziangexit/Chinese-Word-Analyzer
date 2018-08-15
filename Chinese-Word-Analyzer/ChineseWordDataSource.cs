using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Chinese_Word_Analyzer
{
    class ChineseWordDataSource
    {
        public void load(string FileName)
        {
            load(FileName, Encoding.UTF8);
        }

        public void load(string FileName, Encoding encoding)
        {
            string[] FileContent = File.ReadAllLines(FileName, encoding);
            foreach (var p in FileContent)
            {
                string[] cols = p.Split('\t');
                var addMe = new WordDetail();
                addMe.Word = cols[0][0];
                for (int i = 1; i < cols.Length; i++)
                {
                    addMe.Radicals.Add("");
                    string[] cols2 = cols[i].Split(' ');
                    foreach (var p2 in cols2)
                        addMe.Radicals[addMe.Radicals.Count - 1] += p2;
                }
                WordDetails.Add(addMe);
            }
        }

        public class WordDetail
        {
            public char Word { get; set; }
            public List<string> Radicals { get; set; } = new List<string>();
        }

        public List<WordDetail> WordDetails { get; set; } = new List<WordDetail>();
    }
}
