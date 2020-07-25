// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers.Comments
{
    internal class CMultiLineComment : CItem
    {
        private static readonly Regex MULTI_LINE_START = RegexFactory.R(@"^\s*/*");
        private static readonly Regex MULTI_LINE_WHOLE = RegexFactory.R(@"(/\*.*?\*/)");
        private static readonly Regex COMMENT_CONTENT_REGEX = RegexFactory.R(@"\s*([^\r\n]*)");

        private readonly string m_commentString;

        public int CharEndIndexExcluded { get; }

        private CMultiLineComment(string commentString, int endIndex)
        {
            m_commentString = commentString;
            CharEndIndexExcluded = endIndex;
        }

        public string Formatted(int spaceIndent, bool canBreak)
        {
            string[] newLineSplitComment = m_commentString.Split(CFile.LINE_ENDING);

            return newLineSplitComment.Aggregate(
                    new StringBuilder(),
                    (builder, comment) => builder.Append(CFile.Indent(spaceIndent) + COMMENT_CONTENT_REGEX.Match(comment).Groups[1].Value)
                )
                .ToString();
        }

        internal static IOptional<CMultiLineComment> Match(string fileContent, int fileCharIndex)
        {
            Match startMatch = MULTI_LINE_START.Match(fileContent, fileCharIndex);

            if (!startMatch.Success) return Optional<CMultiLineComment>.None;

            Match wholeMatch = MULTI_LINE_WHOLE.Match(fileContent, startMatch.Index + startMatch.Length);

            if (!wholeMatch.Success) return Optional<CMultiLineComment>.None;

            return MULTI_LINE_START.IsMatch(fileContent, fileCharIndex)
                ? Optional.Some(new CMultiLineComment(wholeMatch.Groups[1].Value, wholeMatch.Index + wholeMatch.Length))
                : Optional<CMultiLineComment>.None;
        }
    }
}