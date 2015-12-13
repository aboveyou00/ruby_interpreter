using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class Comment : Whitespace
    {
        public Comment(int start, int length, string source)
            : base(start, length, source)
        {
        }
        
        public override string Stringify()
            => "<%% COMMENT " + SourceCharacters
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") + " %%>";
    }
}
