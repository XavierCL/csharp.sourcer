// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers.Spaces
{
    internal class CBigSpace: CItem
    {
        private static readonly Regex BIG_SPACE_REGEX = RegexFactory.R(@"^\s*\n\s*\n");

        private CBigSpace(int charEndIndexExcluded)
        {
            CharEndIndexExcluded = charEndIndexExcluded;
        }

        public int CharEndIndexExcluded { get; }

        internal static IOptional<CBigSpace> Match(string content, int startIndex)
        {
            var match = BIG_SPACE_REGEX.Match(content, startIndex);

            return match.Success
                ? Optional.Some(new CBigSpace(match.Index + match.Length))
                : Optional<CBigSpace>.None;
        }

        public string Formatted(int previousLineIndent, int spaceIndent, bool canBreak) => CFile.LINE_ENDING + CFile.LINE_ENDING;
    }
}