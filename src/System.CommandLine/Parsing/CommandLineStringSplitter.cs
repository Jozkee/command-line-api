// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.CommandLine.Parsing
{
    /// <summary>
    /// Splits a string based on whitespace and quotation marks
    /// </summary>
    public class CommandLineStringSplitter
    {
        /// <summary>
        /// A single instance of <see cref="CommandLineStringSplitter"/>
        /// </summary>
        public static readonly CommandLineStringSplitter Instance = new();

        private CommandLineStringSplitter()
        {
        }

        private enum Boundary
        {
            TokenStart,
            WordEnd,
            QuoteStart,
            QuoteEnd
        }

        /// <summary>
        /// Splits a string into a sequence of strings based on whitespace and quotation marks.
        /// </summary>
        /// <param name="commandLine">A command line input string.</param>
        /// <returns>A sequence of strings.</returns>
        public IEnumerable<string> Split(string commandLine)
        {
            var memory = commandLine.AsMemory();

            var startTokenIndex = 0;

            var pos = 0;

            var seeking = Boundary.TokenStart;
            var seekingQuote = Boundary.QuoteStart;

            while (pos < memory.Length)
            {
                var c = memory.Span[pos];

                if (char.IsWhiteSpace(c))
                {
                    if (seekingQuote == Boundary.QuoteStart)
                    {
                        switch (seeking)
                        {
                            case Boundary.WordEnd: // this is our word end if we don't have quotation.
                                yield return CurrentToken();
                                startTokenIndex = pos;
                                seeking = Boundary.TokenStart;
                                break;

                            case Boundary.TokenStart: // ignore leading whitespace.
                                startTokenIndex = pos;
                                break;
                        }
                    }
                }
                else if (c == '\"')
                {
                    if (seeking == Boundary.TokenStart)
                    {
                        switch (seekingQuote)
                        {
                            case Boundary.QuoteEnd:
                                // only reset to QuoteStart if `c` wasn't escaped.
                                // this is made because users sometimes meant to escape string within their args, i.e: double-escaping.
                                if (pos > 0 && memory.Span[pos - 1] != '\\')
                                {
                                    yield return CurrentToken();
                                    startTokenIndex = pos;
                                    seekingQuote = Boundary.QuoteStart;
                                }
                                break;
                            case Boundary.QuoteStart: // shouldn't `seeking = WordEnd` here?
                                startTokenIndex = pos + 1;
                                seekingQuote = Boundary.QuoteEnd;
                                break;
                        }
                    }
                    else
                    {
                        switch (seekingQuote)
                        {
                            case Boundary.QuoteEnd:
                                seekingQuote = Boundary.QuoteStart;
                                break;

                            case Boundary.QuoteStart:
                                seekingQuote = Boundary.QuoteEnd;
                                break;
                        }
                    }
                }
                else if (seeking == Boundary.TokenStart && seekingQuote == Boundary.QuoteStart)
                {
                    seeking = Boundary.WordEnd;
                    startTokenIndex = pos;
                }

                Advance();

                if (IsAtEndOfInput())
                {
                    switch (seeking)
                    {
                        case Boundary.TokenStart:
                            break;
                        default:
                            yield return CurrentToken();
                            break;
                    }
                }
            }

            void Advance() => pos++;

            string CurrentToken()
            {
                //string token = memory.Slice(startTokenIndex, IndexOfEndOfToken()).ToString();

                //if (seekingQuote == Boundary.QuoteStart) // don't replace if we have one quote.
                //                                         // NOTE: this won't work when other qouted text is within the same token.
                //                                         // NOTE 2: it should actually work with above since is a different token
                //{
                //    token = token.Replace("\"", "");
                //}

                //return token;
                string token = memory.Slice(startTokenIndex, IndexOfEndOfToken()).ToString();
                return Unescape(token);
            }

            int IndexOfEndOfToken() => pos - startTokenIndex;

            bool IsAtEndOfInput() => pos == memory.Length;

            string Unescape(string currentToken) => currentToken.Replace("\\\"", "\"");
        }
    }
}
