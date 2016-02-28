using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
                            previousTok.PostfixWith(ws);
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
                throw new NotImplementedException();
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
                //Don't increment pos to collect the newline character, we want it as an input element instead
                if (chr == '\n') break;
                else if (chr == '\r' && pos + 1 < _source.Length && _source[pos + 1] == '\n') break;
                else sb.Append(chr);
            }
            _elems.Add(new SingleLineComment(pos - sb.Length - 1, sb.Length + 1, sb.ToString()));
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
                    has_rest = tryParseRestOfBeginEndLineProduction(ref p);
                    if (has_rest) break;
                    if (p >= _source.Length) break;
                    nl = tryParseNewlineProduction(ref p);
                    if (!string.IsNullOrEmpty(nl))
                    {
                        p -= nl.Length;
                        break;
                    }
                    p = origp;
                    sb.Remove(origsbp, sb.Length - origsbp);
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
                    else sb.Append(_source[p++]);
                }
            }
            
            _elems.Add(new MultilineComment(pos, p - pos, sb.ToString()));
            pos = p;
            return true;
        }
        private bool tryParseRestOfBeginEndLineProduction(ref int pos)
        {
            var ws = tryParseWhitespaceProduction(ref pos);
            if (string.IsNullOrEmpty(ws)) return false;
            while ((ws = tryParseWhitespaceProduction(ref pos)) != null) ;
            
            while (pos < _source.Length)
            {
                var nl = tryParseNewlineProduction(ref pos);
                if (!string.IsNullOrEmpty(nl))
                {
                    pos -= nl.Length;
                    break;
                }
                sb.Append(_source[pos++]);
            }

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
            if (tryParseLiteral(ref pos)) return true;
            if (tryParseOperatorOrPunctuator(ref pos)) return true;
            return false;
        }

        private bool tryParseIdentifierOrKeyword(ref int pos)
        {
            bool isGlobal = false,
                 isClass = false,
                 isInstance = false;

            int p = pos;
            var chr = _source[p];
            if (chr == '$')
            {
                p++;
                isGlobal = true;
            }
            if (chr == '@')
            {
                p++;
                if (p < _source.Length && _source[p] == '@')
                {
                    p++;
                    isClass = true;
                }
                else isInstance = true;
            }
            bool hasPrefix = isGlobal || isClass || isInstance;
            if (hasPrefix) chr = _source[p];

            sb.Clear();
            if (!char.IsLetter(chr) && chr != '_') return false;
            sb.Append(chr);
            while (++p < _source.Length)
            {
                chr = _source[p];
                if (char.IsLetterOrDigit(chr) || chr == '_') sb.Append(chr);
                else break;
            }

            pos = p;
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
            bool isNegative = false;
            int p = pos;
            var chr = _source[p];
            if (chr == '-' || chr == '+')
            {
                p++;
                if (chr == '-') isNegative = true;
                for (int q = _elems.Count - 1; q >= 0; q--)
                {
                    if (_elems[q] is Token)
                    {
                        var tok = _elems[q];
                        if (tok is LocalVariableIdentifierToken ||
                            tok is ConstantIdentifierToken ||
                            tok is MethodOnlyIdentifierToken)
                        {
                            bool foundWhitespace = false;
                            for (int w = q + 1; w < _elems.Count; w++)
                                if (_elems[w] is Whitespace) { foundWhitespace = true; break; }
                            if (!foundWhitespace) return false;
                        }
                        break;
                    }
                }
            }

            if (p >= _source.Length) return false;
            if (tryParseFloatLiteral(pos, ref p, isNegative) ||
                tryParseIntegerLiteral(pos, ref p, isNegative))
            {
                pos = p;
                return true;
            }
            else return false;
        }
        private Regex float_unprefixed_decimal_part = new Regex("\\G0|[1-9](_?[0-9])*");
        private Regex float_decimal_digit_part = new Regex("\\G\\.[0-9](_?[0-9])*");
        private Regex float_exponent_part = new Regex("\\G[Ee][+-]?[0-9](_?[0-9])*");
        private bool tryParseFloatLiteral(int start, ref int pos, bool isNegative)
        {
            var p = pos;

            var match = float_unprefixed_decimal_part.Match(_source, start);
            if (match == null || !match.Success) return false;
            p += match.Length;
            sb.Clear();
            if (isNegative) sb.Append("-");
            sb.Append(match.Value);

            bool hasddp = false,
                 hasep = false;

            match = float_decimal_digit_part.Match(_source, p);
            if (match != null && match.Success)
            {
                p += match.Length;
                sb.Append(match.Value);
                hasddp = true;
            }

            match = float_exponent_part.Match(_source, p);
            if (match != null && match.Success)
            {
                p += match.Length;
                sb.Append(match.Value);
                hasep = true;
            }

            if (!hasddp && !hasep) return false;

            pos = p;
            _elems.Add(new FloatLiteralToken(start, sb.ToString(), double.Parse(sb.ToString().Replace("_", ""))));
            return true;
        }
        private bool tryParseIntegerLiteral(int start, ref int pos, bool isNegative)
        {
            var chr = _source[pos];
            if (chr == '0')
            {
                if (++pos < _source.Length)
                {
                    chr = _source[pos];
                    if (chr == 'b' || chr == 'B')
                    {
                        pos++;
                        return tryParseBinaryIntegerLiteral(start, ref pos, isNegative);
                    }
                    else if (chr == '_' || chr == 'o' || chr == 'O' || (chr >= '0' && chr <= '7'))
                    {
                        pos++;
                        return tryParseOctalIntegerLiteral(start, ref pos, isNegative);
                    }
                    else if (chr == 'x' || chr == 'X')
                    {
                        pos++;
                        return tryParseHexadecimalIntegerLiteral(start, ref pos, isNegative);
                    }
                    else if (chr == 'd' || chr == 'D')
                    {
                        pos++;
                        return tryParseDecimalIntegerLiteral(start, ref pos, isNegative);
                    }
                }
                _elems.Add(new IntegerLiteralToken(start, _source.Substring(start, pos - start), BigInteger.Zero));
                return true;
            }
            else if (chr >= '1' && chr <= '9')
                return tryParseDecimalIntegerLiteral(start, ref pos, isNegative);
            else return false;
        }
        private bool tryParseBinaryIntegerLiteral(int start, ref int pos, bool isNegative)
        {
            if (pos >= _source.Length) return false;
            var chr = _source[pos++];
            if (chr < '0' || chr > '1') return false;
            BigInteger val = chr - '0';
            while (pos < _source.Length)
            {
                chr = _source[pos];
                if (chr == '_') break;
                else if (chr >= '0' && chr <= '1')
                {
                    val *= 2;
                    val += chr - '0';
                    pos++;
                }
                else break;
            }
            if (isNegative) val = -val;
            _elems.Add(new IntegerLiteralToken(start, _source.Substring(start, pos - start), val));
            return true;
        }
        private bool tryParseOctalIntegerLiteral(int start, ref int pos, bool isNegative)
        {
            if (pos >= _source.Length) return false;
            var chr = _source[pos++];
            if (chr < '0' || chr > '7') return false;
            BigInteger val = chr - '0';
            while (pos < _source.Length)
            {
                chr = _source[pos];
                if (chr == '_') break;
                else if (chr >= '0' && chr <= '7')
                {
                    val *= 8;
                    val += chr - '0';
                    pos++;
                }
                else break;
            }
            if (isNegative) val = -val;
            _elems.Add(new IntegerLiteralToken(start, _source.Substring(start, pos - start), val));
            return true;
        }
        private bool tryParseDecimalIntegerLiteral(int start, ref int pos, bool isNegative)
        {
            if (pos >= _source.Length) return false;
            var chr = _source[pos++];
            if (chr < '0' || chr > '9') return false;
            BigInteger val = chr - '0';
            while (pos < _source.Length)
            {
                chr = _source[pos];
                if (chr == '_') break;
                else if (chr >= '0' && chr <= '9')
                {
                    val *= 10;
                    val += chr - '0';
                    pos++;
                }
                else break;
            }
            if (isNegative) val = -val;
            _elems.Add(new IntegerLiteralToken(start, _source.Substring(start, pos - start), val));
            return true;
        }
        private bool tryParseHexadecimalIntegerLiteral(int start, ref int pos, bool isNegative)
        {
            if (pos >= _source.Length) return false;
            var chr = char.ToLower(_source[pos++]);
            BigInteger val;
            if (chr >= '0' && chr <= '9') val = chr - '0';
            else if (chr >= 'a' && chr <= 'f') val = chr + 10 - 'a';
            else return false;
            while (pos < _source.Length)
            {
                chr = char.ToLower(_source[pos]);
                if (chr == '_') break;
                else if (chr >= '0' && chr <= '9')
                {
                    val *= 16;
                    val += chr - '0';
                    pos++;
                }
                else if (chr >= 'a' && chr <= 'f')
                {
                    val *= 16;
                    val += chr + 10 - 'a';
                    pos++;
                }
                else break;
            }
            if (isNegative) val = -val;
            _elems.Add(new IntegerLiteralToken(start, _source.Substring(start, pos - start), val));
            return true;
        }

        private bool tryParseStringLiteral(ref int pos)
        {
            var chr = _source[pos];
            if (chr == '\'') return tryParseSingleQuotedString(ref pos);
            else if (chr == '"') return tryParseDoubleQuotedString(ref pos);
            else if (chr == '%')
            {
                if (pos + 1 >= _source.Length) return false;
                else if (_source[pos + 1] == 'q') return tryParseQuotedNonExpandedString(ref pos);
                else if ("Q{([<".Contains(_source[pos + 1])) return tryParseQuotedExpandedString(ref pos);
                else if (_source[pos + 1] == 'x') return tryParseExternalCommand(ref pos);
            }
            else if (chr == '<' && pos + 1 < _source.Length && _source[pos + 1] == '<')
                return tryParseHereDoc(ref pos);
            else if (chr == '`') return tryParseExternalCommand(ref pos);
            return false;
        }
        private bool tryParseSingleQuotedString(ref int pos)
        {
            return false;
            //TODO: tryParseSingleQuotedString
        }
        private bool tryParseDoubleQuotedString(ref int pos)
        {
            return false;
            //TODO: tryParseDoubleQuotedString
        }
        private bool tryParseQuotedNonExpandedString(ref int pos)
        {
            return false;
            //TODO: tryParseQuotedNonExpandedString
        }
        private bool tryParseQuotedExpandedString(ref int pos)
        {
            return false;
            //TODO: tryParseQuotedExpandedString
        }
        private bool tryParseHereDoc(ref int pos)
        {
            return false;
            //TODO: tryParseHereDoc
        }
        private bool tryParseExternalCommand(ref int pos)
        {
            return false;
            //TODO: tryParseExternalCommand
        }

        private bool tryParseArrayLiteral(ref int pos)
        {
            if (pos + 1 >= _source.Length) return false;
            if (_source[pos] != '%') return false;
            if (_source[pos + 1] == 'w') return tryParseNonExpandedArrayConstructor(ref pos);
            else if (_source[pos + 1] == 'W') return tryParseExpandedArrayConstructor(ref pos);
            else return false;
        }
        private bool tryParseNonExpandedArrayConstructor(ref int pos)
        {
            return false;
            //TODO: tryParseNonExpandedArrayConstructor
        }
        private bool tryParseExpandedArrayConstructor(ref int pos)
        {
            return false;
            //TODO: tryParseExpandedArrayConstructor
        }

        private bool tryParseRegularExpressionLiteral(ref int pos)
        {
            if (_source[pos] == '%' && pos + 1 < _source.Length && _source[pos + 1] == 'r')
                return tryParseExpandedRegularExpressionLiteral(ref pos);
            if (_source[pos] != '/') return false;

            return false;
            //TODO: tryParseRegularExpressionLiteral
        }
        private bool tryParseExpandedRegularExpressionLiteral(ref int pos)
        {
            return false;
            //TODO: tryParseExpandedRegularExpressionLiteral
        }

        private bool tryParseSymbolLiteral(ref int pos)
        {
            var chr = _source[pos];
            if (chr == '%' && pos + 1 < _source.Length && _source[pos + 1] == 's') return tryParseNonExpandedSymbol(ref pos);
            if (chr != ':') return false;

            int p = pos + 1;
            if (p >= _source.Length) return false;
            chr = _source[p];

            //TODO: tryParseSymbolLiteral
            if (chr == '\'')
            {
                if (tryParseSingleQuotedSymbol(ref p))
                {
                    pos = p;
                    return true;
                }
            }
            else if (chr == '"')
            {
                if (tryParseDoubleQuotedSymbol(ref p))
                {
                    pos = p;
                    return true;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return false;
        }
        private bool tryParseNonExpandedSymbol(ref int pos)
        {
            return false;
            //TODO: tryParseNonExpandedSymbol
        }
        private bool tryParseSingleQuotedSymbol(ref int pos)
        {
            return false;
            //TODO: tryParseSingleQuotedSymbol
        }
        private bool tryParseDoubleQuotedSymbol(ref int pos)
        {
            return false;
            //TODO: tryParseDoubleQuotedSymbol
        }
    }
}
