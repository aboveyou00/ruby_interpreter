using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeAnalysis.Tokenizer
{
    [TestClass]
    public class TokenizerTest
    {
        private Tokenizer tk = new Tokenizer();
        private Token[] toks;
        private int pos = 0;
        private void UsingString(string str)
        {
            toks = tk.Tokenize(str).ToArray();
            pos = 0;
        }
        private void Expect<T>()
            where T : Token
        {
            var tok = toks[pos++];
            Assert.AreEqual(typeof(T), tok.GetType());
        }
        private void Expect<T>(string source)
        {
            var tok = toks[pos++];
            Assert.AreEqual(typeof(T), tok.GetType());
            Assert.AreEqual(source, tok.SourceCharacters);
        }
        private void Expect<T>(Func<T, bool> condition)
            where T : Token
        {
            var tok = toks[pos++];
            Assert.AreEqual(typeof(T), tok.GetType());
            Assert.IsTrue(condition((T)tok));
        }
        private void Expect<T>(string source, Func<T, bool> condition)
            where T : Token
        {
            var tok = toks[pos++];
            Assert.AreEqual(typeof(T), tok.GetType());
            Assert.AreEqual(source, tok.SourceCharacters);
            Assert.IsTrue(condition((T)tok));
        }
        private void ExpectEnd()
        {
            if (pos < toks.Length) Expect<EndOfProgramToken>();
            Assert.AreEqual(pos, toks.Length);
        }

        [TestMethod]
        public void TestTokenizeEmpty()
        {
            UsingString("");
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeIdentifiersAndKeywords()
        {
            UsingString(@"
local
@instance
@@class
$global
Constant
assignment=
method_e! if method_q?");
            Expect<LocalVariableIdentifierToken>("local", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<InstanceVariableIdentifierToken>("@instance", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<ClassVariableIdentifierToken>("@@class", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<GlobalVariableIdentifierToken>("$global", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<ConstantIdentifierToken>("Constant", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<AssignmentIdentifierToken>("assignment=", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<MethodOnlyIdentifierToken>("method_e!", tok => tok.IsAtBeginningOfLine);
            Expect<KeywordToken>("if");
            Expect<MethodOnlyIdentifierToken>("method_q?", tok => tok.IsAtEndOfLine);
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeWithComments()
        {
            UsingString(@"
a_var #Here's a comment
another_var @instance_var");
            Expect<LocalVariableIdentifierToken>("a_var", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<LocalVariableIdentifierToken>("another_var", tok => tok.IsAtBeginningOfLine);
            Expect<InstanceVariableIdentifierToken>("@instance_var", tok => tok.IsAtEndOfLine);
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeMultilineComments()
        {
            UsingString(@"
=begin This is a multiline comment!
I can write whatever I want here!
=end
local_variable
=begin
=end Or here, I can write here too!");
            Expect<LocalVariableIdentifierToken>("local_variable", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeProgramEnd()
        {
            UsingString(@"
local
$global
__END__
Everything after that will be ignored.");
            Expect<LocalVariableIdentifierToken>("local", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<GlobalVariableIdentifierToken>("$global", tok => tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeOperators()
        {
            UsingString(@"
one * two / three
four << five
six ||= seven");
            Expect<LocalVariableIdentifierToken>("one", tok => tok.IsAtBeginningOfLine);
            Expect<OperatorToken>("*");
            Expect<LocalVariableIdentifierToken>("two");
            Expect<OperatorToken>("/");
            Expect<LocalVariableIdentifierToken>("three", tok => tok.IsAtEndOfLine);
            Expect<LocalVariableIdentifierToken>("four", tok => tok.IsAtBeginningOfLine);
            Expect<OperatorToken>("<<");
            Expect<LocalVariableIdentifierToken>("five", tok => tok.IsAtEndOfLine);
            Expect<LocalVariableIdentifierToken>("six", tok => tok.IsAtBeginningOfLine);
            Expect<OperatorToken>("||=");
            Expect<LocalVariableIdentifierToken>("seven", tok => tok.IsAtEndOfLine);
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeIntegerLiterals()
        {
            UsingString(@"
0
+32
value-24
value -24
0d1234
0b110
0o011
0x256");
            Expect<IntegerLiteralToken>("0", tok => tok.Value == 0 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<IntegerLiteralToken>("+32", tok => tok.Value == 32 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<LocalVariableIdentifierToken>("value", tok => tok.IsAtBeginningOfLine);
            Expect<OperatorToken>("-");
            Expect<IntegerLiteralToken>("24", tok => tok.Value == 24 && tok.IsAtEndOfLine);
            Expect<LocalVariableIdentifierToken>("value", tok => tok.IsAtBeginningOfLine);
            Expect<IntegerLiteralToken>("-24", tok => tok.Value == -24 && tok.IsAtEndOfLine);
            Expect<IntegerLiteralToken>("0d1234", tok => tok.Value == 1234 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<IntegerLiteralToken>("0b110", tok => tok.Value == 6 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<IntegerLiteralToken>("0o011", tok => tok.Value == 9 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<IntegerLiteralToken>("0x256", tok => tok.Value == 0x256 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            ExpectEnd();
        }
        [TestMethod]
        public void TestTokenizeFloatLiterals()
        {
            UsingString(@"
0.0
6.12e2
0e-4
.65
3.2");
            Expect<FloatLiteralToken>("0.0", tok => tok.Value == 0.0 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<FloatLiteralToken>("6.12e2", tok => tok.Value == 6.12e2 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<FloatLiteralToken>("0e-4", tok => tok.Value == 0e-4 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            Expect<PunctuatorToken>(".", tok => tok.IsAtBeginningOfLine);
            Expect<IntegerLiteralToken>("65", tok => tok.Value == 65 && tok.IsAtEndOfLine);
            Expect<FloatLiteralToken>("3.2", tok => tok.Value == 3.2 && tok.IsAtBeginningOfLine && tok.IsAtEndOfLine);
            ExpectEnd();
        }
    }
}
