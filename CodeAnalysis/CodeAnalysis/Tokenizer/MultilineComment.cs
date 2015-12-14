using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class MultilineComment : Comment
    {
        public MultilineComment(int start, int length, string source)
            : base(start, length, source)
        {
        }
        
        public override string Stringify()
            => "=begin\r\n" + SourceCharacters + "=end";
    }
}
