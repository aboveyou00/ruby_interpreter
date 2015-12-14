using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeAnalysis.Tokenizer
{
    public class OperatorToken : Token
    {
        public OperatorToken(int start, string oper)
            : base(start, oper)
        {
            if (oper == null) throw new ArgumentNullException(nameof(oper));
            if (!IsOperator(oper)) throw new NotSupportedException($"{oper} is not a valid operator!");
        }
        
        public bool IsOperatorMethodName => operator_method_names.Contains(SourceCharacters);
        public bool IsAssignmentOperator => assignment_operator_names.Contains(SourceCharacters.Substring(0, SourceCharacters.Length - 1));
        
        private static string[] operators = new string[]
        {
            "!", "!=", "!~", "&&", "||",
            "^", "&", "|", "<=>", "==", "===", "=~", ">", ">=", "<", "<=", "<<", ">>", "+", "-",
            "*", "/", "%", "**", "~", "+@", "-@", "[]", "[]=", "'",
            "=",
            "&&=", "||=", "^=", "&=", "|=", "<<=", ">>=", "+=", "-=", "*=", "/=", "%=", "**="
        };
        private static string[] operator_method_names = new string[]
        {
            "^", "&", "|", "<=>", "==", "===", "=~", ">", ">=", "<", "<=", "<<", ">>", "+", "-",
            "*", "/", "%", "**", "~", "+@", "-@", "[]", "[]=", "'",
        };
        private static string[] assignment_operator_names = new string[]
        {
            "&&", "||", "^", "&", "|", "<<", ">>", "+", "-", "*", "/", "%", "**"
        };
        public static bool IsOperator(string key)
            => operators.Contains(key);
    }
}