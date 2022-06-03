using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Komodo.Utilities;

namespace Komodo.Compilation
{
    public static class Lexer
    {
        static readonly (Regex, Func<string, TokenType>)[] PATTERNS =
        {
            (
                new Regex(@"\G([0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                _ => TokenType.IntLit
            ),
            (
                new Regex(@"\G(\+|-|\*|/)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                text => text switch
                    {
                        "+" => TokenType.Plus,
                        "-" => TokenType.Minus,
                        "*" => TokenType.Asterisk,
                        "/" => TokenType.ForwardSlash,
                        _ => throw new ArgumentException(text)
                    }
            ),
            (
                new Regex(@"\G([\s\S])", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                _ => TokenType.Invalid
            ),
        };

        public static List<Token> Lex(SourceFile sf, Diagnostics? diagnostics = null)
        {
            var tokens = new List<Token>();
            int offset = 0;

            while (offset < sf.Length)
            {
                //Skip Whitespace
                while (Char.IsWhiteSpace(sf.Text[offset]))
                    offset++;

                if (offset >= sf.Length)
                    break;

                //Find best match
                (string, Func<string, TokenType>)? bestMatch = null;

                foreach (var (pattern, func) in PATTERNS)
                {
                    var match = pattern.Match(sf.Text, offset);
                    if (!match.Success)
                        continue;

                    if (!bestMatch.HasValue || match.Value.Length > bestMatch.Value.Item1.Length)
                        bestMatch = (match.Value, func);
                }

                var (value, f) = bestMatch ?? throw new Exception($"'{sf.Text.Substring(offset)}' did not match any pattern!");
                var token = new Token(f(value), sf.GetLocation(offset, offset + value.Length), value);

                if (token.Type == TokenType.Invalid) { diagnostics?.Add(new Diagnostic(DiagnosticType.Error, token.Location, $"Encounterd an invalid token: {token.Value}")); }
                else { tokens.Add(token); }

                offset += value.Length;
            }

            tokens.Add(new Token(TokenType.EOF, sf.GetLocation(offset, offset), ""));
            return tokens;
        }
    }
}