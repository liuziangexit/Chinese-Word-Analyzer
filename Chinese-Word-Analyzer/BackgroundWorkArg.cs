using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chinese_Word_Analyzer
{
    class BackgroundWorkArg
    {
        public enum WorkType
        {
            LoadDataSource
        }

        public object Arg { get; set; }
        public WorkType Type { get; set; }
    }
}
