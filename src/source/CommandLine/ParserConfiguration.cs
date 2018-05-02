using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Cli.CommandLine
{
    public class ParserConfiguration
    {
        public static IReadOnlyCollection<string> DefaultPrefixes { get; } =
            new[] {"-", "--", "/"};

        public ParserConfiguration(
            IReadOnlyCollection<Symbol> definedSymbols,
            IReadOnlyCollection<char> argumentDelimiters = null,
            IReadOnlyCollection<string> prefixes = null,
            bool allowUnbundling = true)
        {
            if (definedSymbols == null)
            {
                throw new ArgumentNullException(nameof(definedSymbols));
            }

            if (!definedSymbols.Any())
            {
                throw new ArgumentException("You must specify at least one option.");
            }

            if (!definedSymbols.OfType<Command>().Any())
            {
                RootCommand = Create.RootCommand(definedSymbols.ToArray());
                DefinedSymbols.Add(RootCommand);
            }
            else
            {
                DefinedSymbols.AddRange(definedSymbols);
            }

            ArgumentDelimiters = argumentDelimiters ?? new[] { ':', '=' };
            AllowUnbundling = allowUnbundling;
            Prefixes = prefixes ?? DefaultPrefixes;

            foreach (Symbol symbol in definedSymbols)
            {
                foreach (string alias in symbol.RawAliases.ToList())
                {
                    if (!Prefixes.All(prefix => alias.StartsWith(prefix)))
                    {
                        foreach (var prefix in Prefixes)
                        {
                            symbol.AddAlias(prefix + alias);
                        }
                    }
                }
            }
        }

        public SymbolSet DefinedSymbols { get; } = new SymbolSet();

        public IReadOnlyCollection<char> ArgumentDelimiters { get; }

        public IReadOnlyCollection<string> Prefixes { get; }

        public bool AllowUnbundling { get; }

        internal Command RootCommand { get; }

        internal bool RootCommandIsImplicit => RootCommand != null;
    }
}
