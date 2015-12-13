using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class GlobalVariableIdentifierToken : IdentifierToken
    {
        public GlobalVariableIdentifierToken(int start, string varName)
            : base(start, "$" + varName)
        {
        }
    }
}
