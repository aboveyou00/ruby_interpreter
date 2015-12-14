using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class EndOfProgramToken : Token
    {
        public EndOfProgramToken(int pos)
            : base(pos, "")
        {
        }

        public override string Stringify()
            => "%% EOP %%";
    }
}
