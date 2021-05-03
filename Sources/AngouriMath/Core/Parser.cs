/* 
 * Copyright (c) 2019-2021 Angouri.
 * AngouriMath is licensed under MIT. 
 * Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
 * Website: https://am.angouri.org.
 */

/*

The parser source files under the Antlr folder other than "Angourimath.g" are generated by ANTLR.
You should only modify "Angourimath.g", other source files are generated from this file.
Any modifications to other source files will be overwritten when the parser is regenerated.

*/

using Antlr4.Runtime;
using System.IO;
using System.Text;

[assembly: System.CLSCompliant(false)]
namespace AngouriMath.Core
{
    using Antlr;
    using Exceptions;
    using System;

    static class Parser
    {
        // Antlr parser spams errors into TextWriter provided, we inherit from it to handle lexer/parser errors as ParseExceptions
        internal sealed class AngouriMathTextWriter : TextWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
            public override void WriteLine(string s) => throw new UnhandledParseException(s);
        }
        public static Entity Parse(string source)
        {
            var lexer = new AngouriMathLexer(new AntlrInputStream(source), null, new AngouriMathTextWriter());
            var tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();
            var tokenList = tokenStream.GetTokens();

            const string NUMBER = nameof(NUMBER);
            const string VARIABLE = nameof(VARIABLE);
            const string PARENTHESIS_OPEN = "'('";
            const string PARENTHESIS_CLOSE = "')'";
            const string FUNCTION_OPEN = "\x1"; // Fake display name for all function tokens e.g. "'sin('"

            static string GetType(IToken token) =>
                AngouriMathLexer.DefaultVocabulary.GetDisplayName(token.Type) is var type
                && type is not PARENTHESIS_OPEN && type.EndsWith("('") ? FUNCTION_OPEN : type;

            if (tokenList.Count == 0)
                throw new AngouriBugException($"{nameof(ParseException)} should have been thrown");
            int i = 0;
            while (tokenList[i].Channel != 0)
                if (++i >= tokenList.Count)
                    goto endTokenInsertion;
            for (int j = i + 1; j < tokenList.Count; i = j++)
            {
                while (tokenList[j].Channel != 0)
                    if (++j >= tokenList.Count)
                        goto endTokenInsertion;
                if ((GetType(tokenList[i]), GetType(tokenList[j])) switch
                {

                    // 2x -> 2 * x       2sqrt -> 2 * sqrt       2( -> 2 * (
                    // x y -> x * y      x sqrt -> x * sqrt      x( -> x * (
                    // )x -> ) * x       )sqrt -> ) * sqrt       )( -> ) * (
                    (NUMBER or VARIABLE or PARENTHESIS_CLOSE, VARIABLE or FUNCTION_OPEN or PARENTHESIS_OPEN) => 
                    MathS.Settings.ExplicitParsingOnly 
                    ? throw new InvalidArgumentParseException("Cannot Multiply '*' When  MathS.Settings.ExplicitParsingOnly.Set(true)  has been called" + $"\n" +
                        "If you want to Multiply without '*' Don't call MathS.Settings.ExplicitParsingOnly.Set(true)") 
                    : lexer.Multiply,
                    // 3 2 -> 3 ^ 2      x2 -> x ^ 2             )2 -> ) ^ 2
                    (NUMBER or VARIABLE or PARENTHESIS_CLOSE, NUMBER) =>
                    MathS.Settings.ExplicitParsingOnly 
                    ? throw new InvalidArgumentParseException("Cannot power a number without '^' When  MathS.Settings.ExplicitParsingOnly.Set(true)  has been called" + $"\n" +
                        "If you want to power a number without '^' Don't call MathS.Settings.ExplicitParsingOnly.Set(true)") 
                    : lexer.Power,

                    _ => null


                } is { } insertToken)
                    // Insert at j because we need to keep the first one behind
                    tokenList.Insert(j, insertToken);
            }
        endTokenInsertion:


            var parser = new AngouriMathParser(tokenStream, null, new AngouriMathTextWriter());
            parser.Parse();
            return parser.Result;
        }

    }
}