﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public abstract class Token : InputElement
    {
        public Token(int start, int length)
            : base(start, length)
        {
            prefixes = new List<InputElement>();
            postfixes = new List<InputElement>();
        }

        private List<InputElement> prefixes, postfixes;
        public void PrefixWith(InputElement elem)
        {
            prefixes.Add(elem);
        }
        public void PostfixWith(InputElement elem)
        {
            postfixes.Add(elem);
        }

        public bool IsPrecededBy<T>()
            where T : Whitespace
            => prefixes.OfType<T>().Any();
        public bool IsFollowedBy<T>()
            where T : Whitespace
            => postfixes.OfType<T>().Any();
        public bool IsAtBeginningOfLine => !prefixes.Any() || prefixes.Last() is LineTerminator;
        public bool IsAtEndOfLine => !postfixes.Any() || postfixes[0] is LineTerminator || postfixes[0] is EndOfProgramToken;

    }
}