using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class LineTerminator : Whitespace
    {
        public LineTerminator(int start, string source)
            : base(start, source)
        {
        }
    }
}
