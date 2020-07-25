// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

namespace CsharpSourcer.Parsers
{
    internal interface CItem
    {
        int CharEndIndexExcluded { get; }

        string Formatted(int previousLineIndent, int spaceIndent, int charSinceWhiteSpace, bool canBreak);
    }
}