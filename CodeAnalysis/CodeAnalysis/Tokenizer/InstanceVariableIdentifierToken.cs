using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class InstanceVariableIdentifierToken : IdentifierToken
    {
        public InstanceVariableIdentifierToken(int start, string varName)
            : base(start, "@" + varName)
        {
        }
    }
}
