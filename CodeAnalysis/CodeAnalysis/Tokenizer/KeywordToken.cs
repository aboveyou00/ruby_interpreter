using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class KeywordToken : Token
    {
        public KeywordToken(int start, string keyword)
            : base(start, keyword)
        {
            if (!IsKeyword(keyword)) throw new NotSupportedException($"{keyword} is not a valid keyword!");
        }
        
        private static string[] keywords = new string[]
        {
            "__LINE__", "__ENCODING__", "__FILE__", "BEGIN", "END", //Reserved for future use
            "alias", "and", "begin", "break", "case", "class", "def", "defined?", "do",
            "else", "elsif", "end", "ensure", "for", "false", "if", "in", "module",
            "next", "nil", "not", "or", "redo", "rescue", "retry", "return", "self",
            "super", "then", "true", "undef", "unless", "until", "when", "while", "yield"
        };
        public static bool IsKeyword(string key)
            => keywords.Contains(key);
    }
}