using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class FloatLiteralToken : Token
    {
        public FloatLiteralToken(int start, string source, double val)
            : base(start, source)
        {
            Value = val;
        }

        public double Value { get; }
    }
}
