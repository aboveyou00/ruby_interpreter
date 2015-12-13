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
            : base(start, punctuator?.Length ?? 0)
        {
            if (punctuator == null) throw new ArgumentNullException(nameof(punctuator));
            if (!IsPunctuator(punctuator)) throw new NotSupportedException($"{punctuator} is not a valid punctuator!");
            Punctuator = punctuator;
        }

        public string Punctuator { get; }

        public override string Stringify()
            => Punctuator;

        private static string[] punctuators = new string[]
        {
            "[", "]", "(", ")", "{", "}", ":", "::", ",", ";", "..", "...", "?", "=>"
        };
        public static bool IsPunctuator(string key)
            => punctuators.Contains(key);
    }
}