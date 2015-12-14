using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class IntegerLiteralToken : Token
    {
        public IntegerLiteralToken(int start, string source, BigInteger val)
            : base(start, source)
        {
            Value = val;
        }

        public BigInteger Value { get; }
    }
}
