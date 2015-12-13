using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public abstract class InputElement
    {
        public InputElement(int start, int length)
        {
            if (start < 0) throw new IndexOutOfRangeException("Input element start index < 0!");
            if (length < 0) throw new IndexOutOfRangeException("Input element length < 0!");
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public abstract string Stringify();
        public override string ToString()
            => $"[{Start},{End}) {Stringify()}";
    }
}
