using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class PunctuatorToken : Token
    {
        public PunctuatorToken(int start, string punctuator)
            : base(start, punctuator)
        {
            if (!IsPunctuator(punctuator)) throw new NotSupportedException($"{punctuator} is not a valid punctuator!");
        }
        
        private static string[] punctuators = new string[]
        {
            "[", "]", "(", ")", "{", "}", ":", "::", ",", ";", "..", "...", "?", "=>"
        };
        public static bool IsPunctuator(string key)
            => punctuators.Contains(key);
    }
}
