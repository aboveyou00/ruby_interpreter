using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class ClassVariableIdentifierToken : IdentifierToken
    {
        public ClassVariableIdentifierToken(int start, string varName)
            : base(start, "@@" + varName)
        {
        }
    }
}
