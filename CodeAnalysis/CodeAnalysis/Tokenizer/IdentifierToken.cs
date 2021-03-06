﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public abstract class IdentifierToken : Token
    {
        public IdentifierToken(int start, string source)
            : base(start, source)
        {
            if (source.Length == 0) throw new NotSupportedException("You can't have an empty identifier!");
        }

        public override string Stringify()
            => SourceCharacters;
    }
}
