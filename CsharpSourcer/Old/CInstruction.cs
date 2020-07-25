// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using CsharpSourcer.Parsers.Brackets;
using CsharpSourcer.Parsers.Comments;
using CsharpSourcer.Parsers.Spaces;
using CsharpSourcer.Parsers.Strings;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers
{
    internal static class CInstruction
    {
        internal static IOptional<CItem> GetNext(string content, int startIndex)
        {
            return CBigSpace.Match(content, startIndex)
                .OrElse<CItem, CItem>(CNewLine.Match(content, startIndex))
                .OrElse<CItem, CItem>(CMultiLineComment.Match(content, startIndex))
                .OrElse<CItem, CItem>(CSingleLineComment.Match(content, startIndex))
                .OrElse<CItem, CItem>(CLiteralString.Match(content, startIndex))
                .OrElse<CItem, CItem>(CString.Match(content, startIndex))
                .OrElse<CItem, CItem>(CTemplate.Match(content, startIndex))
                .OrElse(CParenthesis.Match(content, startIndex))
                .OrElse(CBrace.Match(content, startIndex))
                .OrElse(CNewLineSuggestion.Match(content, startIndex))
                .OrElse(CStatement.Match(content, startIndex));
        }
    }
}