using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class Whitespace : InputElement
    {
        public Whitespace(int start, string source)
            : base(start, source?.Length ?? 0)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            SourceCharacters = source;
        }
        public Whitespace(int start, int length, string source)
            : base(start, length)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            SourceCharacters = source;
        }

        public string SourceCharacters { get; }
        
        public override string Stringify()
            => "<%% WHITESPACE " + SourceCharacters
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") + " %%>";
    }
}
