// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;
using System.CommandLine.Tests.Utility;
using System.IO;
using FluentAssertions;
using Xunit;

namespace System.CommandLine.Tests.Parsing
{
    public class CommandLineStringSplitterTests
    {
        private readonly CommandLineStringSplitter _splitter = CommandLineStringSplitter.Instance;

        [Theory]
        [InlineData("one two three four")]
        [InlineData("one two\tthree   four ")]
        [InlineData(" one two three   four")]
        [InlineData(" one\ntwo\nthree\nfour\n")]
        [InlineData(" one\r\ntwo\r\nthree\r\nfour\r\n")]
        public void It_splits_strings_based_on_whitespace(string commandLine)
        {
            _splitter.Split(commandLine)
                     .Should()
                     .BeEquivalentSequenceTo("one", "two", "three", "four");
        }

        [Fact]
        public void It_does_not_break_up_double_quote_delimited_values()
        {
            var commandLine = @"rm -r ""c:\temp files\""";

            _splitter.Split(commandLine)
                     .Should()
                     .BeEquivalentSequenceTo("rm", "-r", @"c:\temp files\");
        }

        [Theory]
        [InlineData("-", '=')]
        [InlineData("-", ':')]
        [InlineData("--", '=')]
        [InlineData("--", ':')]
        [InlineData("/", '=')]
        [InlineData("/", ':')]
        public void It_does_not_split_double_quote_delimited_values_when_a_non_whitespace_argument_delimiter_is_used(
            string prefix,
            char delimiter)
        {
            var optionAndArgument = $@"{prefix}the-option{delimiter}""c:\temp files\""";

            var commandLine = $"the-command {optionAndArgument}";

            _splitter.Split(commandLine)
                     .Should()
                     .BeEquivalentSequenceTo("the-command", optionAndArgument.Replace("\"", ""));
        }

        [Fact]
        public void It_handles_multiple_options_with_quoted_arguments()
        {
            var source = Directory.GetCurrentDirectory();
            var destination = Path.Combine(Directory.GetCurrentDirectory(), ".trash");

            var commandLine = $"move --from \"{source}\" --to \"{destination}\" --verbose";

            var tokenized = _splitter.Split(commandLine);

            tokenized.Should()
                     .BeEquivalentSequenceTo(
                         "move",
                         "--from",
                         source,
                         "--to",
                         destination,
                         "--verbose");
        }

        [Fact]
        public void Internal_quotes_do_not_cause_string_to_be_split()
        {
            var commandLine = @"POST --raw='{""Id"":1,""Name"":""Alice""}'";

            _splitter.Split(commandLine)
                     .Should()
                     .BeEquivalentTo("POST", "--raw='{Id:1,Name:Alice}'");
        }

        [Fact]
        public void Internal_whitespaces_are_preserved_and_do_not_cause_string_to_be_split()
        {
            var commandLine = @"command --raw='{""Id"":1,""Movie Name"":""The Three Musketeers""}'";

            _splitter.Split(commandLine)
                     .Should()
                     .BeEquivalentTo("command", "--raw='{Id:1,Movie Name:The Three Musketeers}'");
        }

        [Theory]
        [InlineData(@"D:\", @"D:\")]
        [InlineData(@"\\server\share\path", @"\\server\share\path")]
        [InlineData(@"""\\server\share\path with spaces""", @"\\server\share\path with spaces")]
        [InlineData(@"""abc"" d e", @"abc,d,e")]
        [InlineData(@"a\\\b d""e f""g h", @"a\\\b,de fg,h")]
        [InlineData(@"a\\\""b c d", @"a\""b,c,d")]
        [InlineData(@"a\\\\""b c"" d e", @"a\\b c,d,e")]
        // custom-made cases
        [InlineData(@"foo""", @"foo")] // ignored quote, cmd does ignore it.
        [InlineData(@"foo\""", @"foo""")] // ignored quote, cmd does not ignore it.

        // trailing quote unclosed
        // leading quote unclosed
        // all of the above with a leading/trailing quoted argument

        // https://github.com/dotnet/command-line-api/issues/1740 double escaped.
        [InlineData(
            "\"dotnet publish \\\"xxx.csproj\\\" -c Release -o \\\"./bin/latest/\\\" -r linux-x64 --self-contained false\"",
            "dotnet publish \"xxx.csproj\" -c Release -o \"./bin/latest/\" -r linux-x64 --self-contained false")]
        // altered version with less escaping
        [InlineData(
            "dotnet publish \"xxx.csproj\" -c Release -o \"./bin/latest/\" -r linux-x64 --self-contained false",
            "dotnet,publish,xxx.csproj,-c,Release,-o,./bin/latest/,-r,linux-x64,--self-contained,false")]
        public void It_does_preserve_non_escaping_backslashes(string commandLine, string commaSeparatedResult)
        {
            string[] expected = commaSeparatedResult.Split(','); 
            _splitter.Split(commandLine)
                     .Should()
                     .BeEquivalentTo(expected);
        }
    }
}
