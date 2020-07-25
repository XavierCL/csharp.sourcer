// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers.Strings
{
    internal class CLiteralString : CItem
    {
        private static readonly Regex LITERAL_STRING_START = RegexFactory.R(@"^\s*(@"")");
        private readonly string m_literal;

        public int CharEndIndexExcluded { get; }

        private CLiteralString(string literal, int endIndex)
        {
            m_literal = literal;

            CharEndIndexExcluded = endIndex;
        }

        public string Formatted(int spaceIndent, bool canBreak) => CFile.Indent(spaceIndent) + m_literal;

        public static IOptional<CLiteralString> Match(string content, int startIndex)
        {
            Match match = LITERAL_STRING_START.Match(content, startIndex);

            if (!match.Success) return Optional<CLiteralString>.None;

            int literalScanIndex = match.Index + match.Length;

            while (literalScanIndex < content.Length)
            {
                if (content[literalScanIndex] == '"' && literalScanIndex < content.Length - 1 && content[literalScanIndex + 1] == '"')
                {
                    literalScanIndex += 2;
                }

                else if (content[literalScanIndex] == '"')
                {
                    ++literalScanIndex;
                    break;
                }

                else
                {
                    ++literalScanIndex;
                }
            }

            return Optional.Some(new CLiteralString(content.Substring(match.Groups[1].Index, literalScanIndex), literalScanIndex));
        }
    }
}