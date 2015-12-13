using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class Tokenizer
    {
        public Tokenizer()
        {

        }

        private object SynchronizationObject { get; } = new object();

        public IEnumerable<Token> Tokenize(string source)
        {
            InputElement[] elements;
            lock (SynchronizationObject)
            {
                _source = source;
                parseInputElements();
                elements = _elems.ToArray();
            }
            var collector = new List<Whitespace>();
            Token previousTok = null;
            foreach (var elem in elements)
            {
                if (elem == null) throw new InvalidProgramException("Why is there a null value in the input stream?");
                if (elem is Whitespace)
                    collector.Add(elem as Whitespace);
                else if (elem is Token)
                {
                    var tok = (Token)elem;
                    if (previousTok != null)
                    {
                        foreach (var ws in collector)
                            tok.PostfixWith(ws);
                        previousTok.PostfixWith(tok);
                        tok.PrefixWith(previousTok);
                    }
                    foreach (var ws in collector)
                        tok.PrefixWith(ws);
                    previousTok = tok;
                    collector.Clear();
                    yield return tok;
                }
                else throw new NotSupportedException($"Input element of type {elem.GetType().Name}");
            }
        }

        private string _source;
        private List<InputElement> _elems = new List<InputElement>();
        StringBuilder sb = new StringBuilder();

        private void parseInputElements()
        {
            //Newline : [\r]\n
            //Whitespace: 0x09 | 0x0B | 0x0C | 0x0D | 0x20 | \[\r]\n
            //Comment:
            //    #(then char* until end of line)
            //    (line beginning)=begin[Whitespace,then char* until end of line]Newline
            //        (char* then Newline)*
            //        (line beginning)=end[Whitespace,then char* until end of line or file](Newline|EOF)

            int pos = 0;
            while (pos < _source.Length)
            {
                if (tryParseNewlineElement(ref pos)) continue;
                if (tryParseWhitespaceElement(ref pos)) continue;
                if (tryParseCommentElement(ref pos)) continue;
                if (tryParseEarlyEndOfProgram(ref pos)) return;
                if (tryParseToken(ref pos)) continue;

                //TODO: Emit ErrorToken
            }
            _elems.Add(new EndOfProgramToken(pos));
        }

        private bool tryParseNewlineElement(ref int pos)
        {
            var nl = tryParseNewlineProduction(ref pos);
            if (string.IsNullOrEmpty(nl)) return false;
            _elems.Add(new LineTerminator(pos - nl.Length, nl));
            return true;
        }
        private string tryParseNewlineProduction(ref int pos)
        {
            if (pos >= _source.Length) return null;
            else if (_source[pos] == '\n')
            {
                pos += 1;
                return "\n";
            }
            else if (_source[pos] == '\r' && pos + 1 < _source.Length && _source[pos + 1] == '\n')
            {
                pos += 2;
                return "\r\n";
            }
            return null;
        }
        private bool tryParseWhitespaceElement(ref int pos)
        {
            var ws = tryParseWhitespaceProduction(ref pos);
            if (string.IsNullOrEmpty(ws)) return false;

            sb.Clear();
            sb.Append(ws);
            while ((ws = tryParseWhitespaceProduction(ref pos)) != null)
                sb.Append(ws);

            _elems.Add(new Whitespace(pos - sb.Length, sb.ToString()));
            return true;
        }
        private string tryParseWhitespaceProduction(ref int pos)
        {
            if (pos >= _source.Length) return null;
            var chr = _source[pos];
            if ("\x09\x0b\x0c\x0d\x20".Contains(chr))
            {
                pos += 1;
                return chr.ToString();
            }
            else if (chr == '\\')
            {
                int p = pos + 1;
                var nl = tryParseNewlineProduction(ref p);
                if (string.IsNullOrEmpty(nl)) return null;
                pos = p;
                return $"\\{nl}";
            }
            else return null;
        }
        private bool tryParseEarlyEndOfProgram(ref int pos)
        {
            if (_source[pos] != '_') return false;
            if (!isBeginningOfLine()) return false;

            int p = pos;
            if (!tryParseExact(ref p, "__END__")) return false;

            var nl = tryParseNewlineProduction(ref p);
            if (p == _source.Length || !string.IsNullOrEmpty(nl))
            {
                pos = p;
                return true;
            }
            return false;
        }
        
        private bool tryParseCommentElement(ref int pos)
        {
            if (tryParseSinglelineComment(ref pos)) return true;
            if (tryParseMultilineComment(ref pos)) return true;
            return false;
        }
        private bool tryParseSinglelineComment(ref int pos)
        {
            if (_source[pos] != '#') return false;
            sb.Clear();
            while (++pos < _source.Length)
            {
                var chr = _source[pos];
                if (chr == '\n') break;
                else if (chr == '\r' && pos + 1 < _source.Length && _source[pos + 1] == '\n')
                {
                    pos++;
                    break;
                }
                else sb.Append(chr);
            }
            _elems.Add(new Comment(pos - sb.Length - 1, sb.Length + 1, sb.ToString()));
            return true;
        }
        private bool tryParseMultilineComment(ref int pos)
        {
            if (_source[pos] != '=') return false;
            if (!isBeginningOfLine()) return false;

            //multi-line-comment-begin-line
            int p = pos;
            if (!tryParseExact(ref p, "=begin")) return false;
            sb.Clear();
            var has_rest = tryParseRestOfBeginEndLineProduction(ref p);
            var nl = tryParseNewlineProduction(ref p);
            if (string.IsNullOrEmpty(nl)) return false;
            if (has_rest) sb.Append(nl);

            while (true)
            {
                if (tryParseExact(ref p, "=end"))
                {
                    //multi-line-comment-end-line
                    int origp = p - 4;
                    int origsbp = sb.Length;
                    tryParseRestOfBeginEndLineProduction(ref p);
                    if (p >= _source.Length || !string.IsNullOrEmpty(tryParseNewlineProduction(ref p)))
                        break;
                    else
                    {
                        p = origp;
                        sb.Remove(origsbp, sb.Length - origsbp);
                    }
                }

                //multi-line-comment-line
                while (p < _source.Length)
                {
                    nl = tryParseNewlineProduction(ref p);
                    if (!string.IsNullOrEmpty(nl))
                    {
                        sb.Append(nl);
                        break;
                    }
                    else sb.Append(_source[p]);
                }
            }
            
            _elems.Add(new Comment(pos, p - pos, sb.ToString()));
            pos = p;
            return true;
        }
        private bool tryParseRestOfBeginEndLineProduction(ref int pos)
        {
            var ws = tryParseWhitespaceProduction(ref pos);
            if (string.IsNullOrEmpty(ws)) return false;
            while ((ws = tryParseWhitespaceProduction(ref pos)) != null) ;

            while (pos < _source.Length && string.IsNullOrEmpty(tryParseNewlineProduction(ref pos)))
                sb.Append(_source[pos]);

            return true;
        }

        private bool isBeginningOfLine()
        {
            if (_elems.Count == 0) return true;
            else if (_elems.Last() is LineTerminator) return true;
            else if (_elems.Last() is Whitespace && ((Whitespace)_elems.Last()).SourceCharacters.EndsWith("\n")) return true;
            return false;
        }
        private bool tryParseExact(ref int pos, string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (pos + str.Length > _source.Length) return false;
            
            for (int q = 0; q < str.Length; q++)
                if (str[q] != _source[pos + q]) return false;
            pos += str.Length;
            return true;
        }

        private bool tryParseToken(ref int pos)
        {
            if (tryParseIdentifierOrKeyword(ref pos)) return true;
            if (tryParseOperatorOrPunctuator(ref pos)) return true;
            if (tryParseLiteral(ref pos)) return true;
            return false;
        }

        private bool tryParseIdentifierOrKeyword(ref int pos)
        {
            bool isGlobal = false,
                 isClass = false,
                 isInstance = false;

            var chr = _source[pos];
            if (chr == '$') isGlobal = true;
            if (chr == '@')
            {
                if (pos + 1 < _source.Length && _source[pos + 1] == '@') isClass = true;
                else isInstance = true;
            }
            bool hasPrefix = isGlobal || isClass || isInstance;

            sb.Clear();
            if (!char.IsLetter(chr) && chr != '_') return false;
            sb.Append(chr);
            while (++pos < _source.Length)
            {
                chr = _source[pos];
                if (char.IsLetterOrDigit(chr) || chr == '_') sb.Append(chr);
                else break;
            }

            if (hasPrefix)
            {
                if (isGlobal) _elems.Add(new GlobalVariableIdentifierToken(pos - sb.Length - 1, sb.ToString()));
                if (isClass) _elems.Add(new ClassVariableIdentifierToken(pos - sb.Length - 2, sb.ToString()));
                if (isInstance) _elems.Add(new InstanceVariableIdentifierToken(pos - sb.Length - 1, sb.ToString()));
                return true;
            }
            else if (KeywordToken.IsKeyword(sb.ToString()))
            {
                _elems.Add(new KeywordToken(pos - sb.Length, sb.ToString()));
                return true;
            }
            if (pos < _source.Length)
            {
                if (chr == '=')
                {
                    pos++;
                    _elems.Add(new AssignmentIdentifierToken(pos - sb.Length - 1, sb.ToString()));
                    return true;
                }
                else if (chr == '?' || chr == '!')
                {
                    pos++;
                    sb.Append(chr);
                    _elems.Add(new MethodOnlyIdentifierToken(pos - sb.Length, sb.ToString()));
                    return true;
                }
            }

            if (char.IsLetter(sb[0]) && char.IsUpper(sb[0]))
                _elems.Add(new ConstantIdentifierToken(pos - sb.Length, sb.ToString()));
            else _elems.Add(new LocalVariableIdentifierToken(pos - sb.Length, sb.ToString()));
            return true;
        }
        
        //private bool tryParsePunctuator(ref int pos)
        //{
        //    var chr = _source[pos];
        //    if ("[](){},;?".Contains(chr))
        //    {
        //        _elems.Add(new PunctuatorToken(pos++, chr.ToString()));
        //        return true;
        //    }
        //    else if (chr == ':')
        //    {
        //        if (pos + 1 < _source.Length && _source[pos + 1] == ':')
        //        {
        //            _elems.Add(new PunctuatorToken(pos, "::"));
        //            pos += 2;
        //            return true;
        //        }
        //        else
        //        {
        //            _elems.Add(new PunctuatorToken(pos++, ":"));
        //            return true;
        //        }
        //    }
        //    else if (chr == '.')
        //    {
        //        if (pos + 1 < _source.Length && _source[pos + 1] == '.')
        //        {
        //            if (pos + 2 < _source.Length && _source[pos + 2] == '.')
        //            {
        //                _elems.Add(new PunctuatorToken(pos, "..."));
        //                pos += 3;
        //                return true;
        //            }
        //            else
        //            {
        //                _elems.Add(new PunctuatorToken(pos, ".."));
        //                pos += 2;
        //                return true;
        //            }
        //        }
        //        else
        //        {
        //            _elems.Add(new PunctuatorToken(pos++, "."));
        //            return true;
        //        }
        //    }
        //    else if (chr == '=' && pos + 1 < _source.Length && _source[pos + 1] == '>')
        //    {
        //        _elems.Add(new PunctuatorToken(pos, "=>"));
        //        pos += 2;
        //        return true;
        //    }
        //    return false;
        //}
        
        private bool tryParseOperatorOrPunctuator(ref int pos)
        {
            var op = tryParseOperatorOrPunctuatorProduction(ref pos);
            if (string.IsNullOrEmpty(op)) return false;
            if (OperatorToken.IsOperator(op)) _elems.Add(new OperatorToken(pos - op.Length, op));
            else if (PunctuatorToken.IsPunctuator(op)) _elems.Add(new PunctuatorToken(pos - op.Length, op));
            else throw new InvalidOperationException("tryParseOperatorProduction returned a value which is neither an operator or a punctuator.");
            return true;
        }
        private string tryParseOperatorOrPunctuatorProduction(ref int pos)
        {
            if (pos >= _source.Length) return null;
            sb.Clear();

            char chr = _source[pos];
            if ("](){}~,;?'".Contains(chr)) sb.Append(chr);
            else if (chr == '!')
            {
                //    !   !=  !~
                sb.Append(chr);
                if (pos + 1 < _source.Length)
                {
                    chr = _source[pos + 1];
                    if (chr == '=' || chr == '~') sb.Append(chr);
                }
            }
            else if (chr == '&' || chr == '|' || chr == '>' || chr == '*')
            {
                //    &   &=  &&  &&= 
                //    |   |=  ||  ||=
                //    >   >=  >>  >>=
                //    *   *=  **  **=
                sb.Append(chr);
                if (pos + 1 < _source.Length)
                {
                    var chr2 = _source[pos + 1];
                    if (chr2 == '=') sb.Append(chr2);
                    else if (chr2 == chr)
                    {
                        sb.Append(chr);
                        if (pos + 2 < _source.Length && _source[pos + 2] == '=') sb.Append('=');
                    }
                }
            }
            else if (chr == '=')
            {
                //    =   =~  ==  === =>
                sb.Append(chr);
                if (pos + 1 < _source.Length)
                {
                    chr = _source[pos + 1];
                    if (chr == '~' || chr == '>') sb.Append(chr);
                    else if (chr == '=')
                    {
                        sb.Append(chr);
                        if (pos + 2 < _source.Length && _source[pos + 2] == '=') sb.Append('=');
                    }
                }
            }
            else if (chr == '<')
            {
                //    <   <=  <=> <<  <<=
                sb.Append(chr);
                if (pos + 1 < _source.Length)
                {
                    chr = _source[pos + 1];
                    if (chr == '=')
                    {
                        sb.Append(chr);
                        if (pos + 2 < _source.Length && _source[pos + 2] == '>') sb.Append('>');
                    }
                    else if (chr == '<')
                    {
                        sb.Append(chr);
                        if (pos + 2 < _source.Length && _source[pos + 2] == '=') sb.Append('=');
                    }
                }
            }
            else if (chr == '^' || chr == '/' || chr == '%')
            {
                //    ^   ^=
                //    /   /=
                //    %   %=
                sb.Append(chr);
                if (pos + 1 < _source.Length && _source[pos + 1] == '=') sb.Append('=');
            }
            else if (chr == '+' || chr == '-')
            {
                //    +   +=  +@
                //    -   -=  -@
                sb.Append(chr);
                if (pos + 1 < _source.Length)
                {
                    chr = _source[pos + 1];
                    if (chr == '=' || chr == '@') sb.Append(chr);
                }
            }
            else if (chr == ':')
            {
                //    :       ::
                sb.Append(chr);
                if (pos + 1 < _source.Length && _source[pos + 1] == ':') sb.Append(':');
            }
            else if (chr == '.')
            {
                //    .       ..  ...
                sb.Append(chr);
                if (pos + 1 < _source.Length && _source[pos + 1] == '.')
                {
                    sb.Append('.');
                    if (pos + 2 < _source.Length && _source[pos + 2] == '.') sb.Append('.');
                }
            }
            else if (chr == '[')
            {
                //    [   []      []=
                sb.Append(chr);
                if (pos + 1 < _source.Length && _source[pos + 1] == ']')
                {
                    sb.Append(']');
                    if (pos + 2 < _source.Length && _source[pos + 2] == '=') sb.Append('=');
                }
            }

            pos += sb.Length;
            return sb.ToString();
        }

        private bool tryParseLiteral(ref int pos)
        {
            if (tryParseNumericLiteral(ref pos)) return true;
            if (tryParseStringLiteral(ref pos)) return true;
            if (tryParseArrayLiteral(ref pos)) return true;
            if (tryParseRegularExpressionLiteral(ref pos)) return true;
            if (tryParseSymbolLiteral(ref pos)) return true;
            return false;
        }
        private bool tryParseNumericLiteral(ref int pos)
        {
            return false;
            //TODO: tryParseNumericLiteral
        }
        private bool tryParseStringLiteral(ref int pos)
        {
            return false;
            //TODO: tryParseStringLiteral
        }
        private bool tryParseArrayLiteral(ref int pos)
        {
            return false;
            //TODO: tryParseArrayLiteral
        }
        private bool tryParseRegularExpressionLiteral(ref int pos)
        {
            return false;
            //TODO: tryParseRegularExpressionLiteral
        }
        private bool tryParseSymbolLiteral(ref int pos)
        {
            return false;
            //TODO: tryParseSymbolLiteral
        }
    }
}
