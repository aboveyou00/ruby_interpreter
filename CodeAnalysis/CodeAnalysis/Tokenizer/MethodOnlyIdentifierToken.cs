using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class MethodOnlyIdentifierToken : IdentifierToken
    {
        public MethodOnlyIdentifierToken(int start, string varName)
            : base(start, varName)
        {
            var chr = varName[varName.Length - 1];
            if (chr != '?' && chr != '!')
                throw new NotSupportedException($"{varName} is not a valid method-only identifier name.");
        }
    }
}
