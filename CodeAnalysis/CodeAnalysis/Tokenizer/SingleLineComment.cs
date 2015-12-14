using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class SingleLineComment : Comment
    {
        public SingleLineComment(int start, int length, string source)
            : base(start, length, source)
        {
        }
        
        public override string Stringify()
            => "#" + SourceCharacters;
    }
}
