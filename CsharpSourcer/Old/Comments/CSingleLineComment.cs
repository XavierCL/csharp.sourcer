// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers.Comments
{
    internal class CSingleLineComment : CItem
    {
        private static readonly Regex SINGLE_LINE_COMMENT_WHOLE = RegexFactory.R(@"^\s*?(//[^\r\n]*)");

        private readonly string m_comment;

        public int CharEndIndexExcluded { get; }

        private CSingleLineComment(string comment, int endIndex)
        {
            m_comment = comment;
            CharEndIndexExcluded = endIndex;
        }

        public string Formatted(int spaceIndent, bool canBreak) => CFile.Indent(spaceIndent) + m_comment;

        internal static IOptional<CSingleLineComment> Match(string fileContent, int fileCharIndex)
        {
            Match match = SINGLE_LINE_COMMENT_WHOLE.Match(fileContent, fileCharIndex);

            return match.Success
                ? Optional.Some(new CSingleLineComment(match.Groups[1].Value, match.Index + match.Length))
                : Optional<CSingleLineComment>.None;
        }
    }
}