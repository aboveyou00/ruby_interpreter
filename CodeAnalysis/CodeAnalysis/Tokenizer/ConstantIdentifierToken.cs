using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class ConstantIdentifierToken : IdentifierToken
    {
        public ConstantIdentifierToken(int start, string varName)
            : base(start, varName)
        {
            var chr = varName[0];
            if (!char.IsLetter(chr) || !char.IsUpper(chr))
                throw new NotSupportedException($"{varName} is not a valid constant identifier name.");
        }
    }
}
