// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers
{
    public interface StringParser : IParser<string>
    {}

    public class RegexParser : Parser<string>, StringParser
    {
        private readonly Regex m_regex;

        public RegexParser(string pattern)
        {
            m_regex = new Regex("^" + pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        public override IEither<ParsingException, IPartialParse<string>> Parse(string input)
        {
            Match match = m_regex.Match(input);

            if (!match.Success) return Either.Create<IPartialParse<string>>.Left(ParsingFailure(input));

            return Either.Create<ParsingException>.Right(new PartialParse<string>(match.Value, input.Substring(match.Index + match.Length)));
        }

        public override string GetRepresentation(HashSet<Guid> alreadyPrinted) => $@"{{""regex"": ""{m_regex}""";
    }

    public class LiteralParser : Parser<string>, StringParser
    {
        private readonly string m_literal;

        public LiteralParser(string literal)
        {
            m_literal = literal;
        }

        public override IEither<ParsingException, IPartialParse<string>> Parse(string input) =>
            input.StartsWith(m_literal, StringComparison.InvariantCulture)
                ? Either.Create<ParsingException>.Right(new PartialParse<string>(m_literal, input.Substring(m_literal.Length)))
                : Either.Create<IPartialParse<string>>.Left<ParsingException>(ParsingFailure(input));

        public override string GetRepresentation(HashSet<Guid> alreadyPrinted) => $@"{{""literal"": ""{m_literal}""";
    }
}