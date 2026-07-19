using System;
using System.Collections.Generic;
using System.Text;

namespace CsgoItemsParser.Parser
{
    /// <summary>
    /// Minimal recursive-descent parser for Valve's KeyValues ("VDF") text format,
    /// the format used by items_game.txt and csgo_english.txt.
    ///
    /// Regex over the raw text cannot reliably tell nodes apart at different nesting
    /// depths or cope with varying field order/presence, which is why the old
    /// regex-based extraction silently dropped the vast majority of item definitions.
    /// Parsing the real key/value tree and walking it structurally fixes that.
    /// </summary>
    public static class VdfParser
    {
        private enum TokenKind
        {
            String,
            Open,
            Close
        }

        private struct Token
        {
            public TokenKind Kind;
            public string Value;
        }

        public static KeyValue Parse(string text)
        {
            var tokens = Tokenize(text);
            int position = 0;

            var root = new KeyValue { Key = "$root", Children = ParseBlock(tokens, ref position) };
            return root;
        }

        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            int i = 0;
            int length = text.Length;

            while (i < length)
            {
                char c = text[i];

                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    i++;
                    continue;
                }

                // Line comment
                if (c == '/' && i + 1 < length && text[i + 1] == '/')
                {
                    int newline = text.IndexOf('\n', i);
                    i = newline == -1 ? length : newline;
                    continue;
                }

                if (c == '"')
                {
                    int j = i + 1;
                    var buffer = new StringBuilder();

                    while (j < length && text[j] != '"')
                    {
                        if (text[j] == '\\' && j + 1 < length)
                        {
                            buffer.Append(text[j + 1]);
                            j += 2;
                            continue;
                        }

                        buffer.Append(text[j]);
                        j++;
                    }

                    tokens.Add(new Token { Kind = TokenKind.String, Value = buffer.ToString() });
                    i = j + 1;
                    continue;
                }

                if (c == '{')
                {
                    tokens.Add(new Token { Kind = TokenKind.Open });
                    i++;
                    continue;
                }

                if (c == '}')
                {
                    tokens.Add(new Token { Kind = TokenKind.Close });
                    i++;
                    continue;
                }

                // Conditional tag, e.g. [$WIN32] - not relevant to structure, skip it.
                if (c == '[')
                {
                    int close = text.IndexOf(']', i);
                    i = close == -1 ? length : close + 1;
                    continue;
                }

                // Unquoted token (rare in practice, but valid VDF).
                {
                    int j = i;
                    while (j < length && !IsTerminator(text[j])) j++;

                    if (j == i)
                    {
                        // Unrecognized character, skip it to guarantee progress.
                        i++;
                        continue;
                    }

                    tokens.Add(new Token { Kind = TokenKind.String, Value = text.Substring(i, j - i) });
                    i = j;
                }
            }

            return tokens;
        }

        private static bool IsTerminator(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '{' || c == '}' || c == '"';
        }

        private static List<KeyValue> ParseBlock(List<Token> tokens, ref int position)
        {
            var children = new List<KeyValue>();

            while (position < tokens.Count)
            {
                var token = tokens[position];

                if (token.Kind == TokenKind.Close)
                {
                    position++;
                    return children;
                }

                if (token.Kind != TokenKind.String)
                {
                    // Stray '{' with no preceding key - skip defensively.
                    position++;
                    continue;
                }

                string key = token.Value;
                position++;

                if (position >= tokens.Count)
                {
                    children.Add(new KeyValue { Key = key, Value = string.Empty });
                    break;
                }

                var next = tokens[position];

                if (next.Kind == TokenKind.Open)
                {
                    position++;
                    var nested = ParseBlock(tokens, ref position);
                    children.Add(new KeyValue { Key = key, Children = nested });
                }
                else if (next.Kind == TokenKind.String)
                {
                    position++;
                    children.Add(new KeyValue { Key = key, Value = next.Value });
                }
                else
                {
                    // A key followed directly by '}' with no value - treat as empty leaf.
                    children.Add(new KeyValue { Key = key, Value = string.Empty });
                }
            }

            return children;
        }
    }
}
