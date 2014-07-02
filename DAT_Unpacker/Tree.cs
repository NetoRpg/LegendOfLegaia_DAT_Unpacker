using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAT_Unpacker
{
    class Tree
    {
            public string name;
            public byte[] header = new byte[4];
            //public int timNum;
            public List<string> tims = new List<string>();
    }
}
