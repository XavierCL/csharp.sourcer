// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers.Spaces
{
    internal class CNewLine: CItem
    {
        private static readonly Regex NEW_LINE_REGEX = RegexFactory.R(@"^\s*\n");

        public int CharEndIndexExcluded { get; }

        private CNewLine(int charEndIndexExcluded)
        {
            CharEndIndexExcluded = charEndIndexExcluded;
        }

        public string Formatted(int previousLineIndent, int spaceIndent, bool canBreak) => CFile.LINE_ENDING;

        internal static IOptional<CNewLine> Match(string content, int startIndex)
        {
            Match match = NEW_LINE_REGEX.Match(content, startIndex);

            return match.Success
                ? Optional.Some(new CNewLine(match.Index + match.Length))
                : Optional<CNewLine>.None;
        }
    }
}