using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class LocalVariableIdentifierToken : IdentifierToken
    {
        public LocalVariableIdentifierToken(int start, string varName)
            : base(start, varName)
        {
            var chr = varName[0];
            if ((!char.IsLetter(chr) || !char.IsLower(chr)) && chr != '_')
                throw new NotSupportedException($"{varName} is not a valid local variable identifier name.");
        }
    }
}
