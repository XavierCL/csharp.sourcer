// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System;

namespace CsharpSourcer.Parsers
{
    internal class CAny : CItem
    {
        private readonly string m_content;

        public CAny(string content, int charEndIndexExcluded)
        {
            m_content = content;
            CharEndIndexExcluded = charEndIndexExcluded;
        }

        public int CharEndIndexExcluded { get; }

        public string Formatted(int spaceIndent, bool canBreak) => CFile.Indent(spaceIndent) + m_content;
    }
}