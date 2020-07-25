// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace CsharpSourcer.Parsers.Brackets
{
    internal class CBrace : CItem
    {
        private readonly ImmutableList<CItem> m_items;

        public int CharEndIndexExcluded => m_items.LastOrDefault()?.CharEndIndexExcluded ?? 0;

        internal CBrace(string fileContent, int endOfMatchExclusive)
        {
            int charIndex = endOfMatchExclusive;
            var items = new List<CItem>();

            bool NextItem(CItem item)
            {
                items.Add(item);
                charIndex = item.CharEndIndexExcluded;
                return true;
            }

            static bool NoItem() => false;

            while (CInstruction.GetNext(fileContent, charIndex).Match(NextItem, NoItem))
            {}

            m_items = items.ToImmutableList();
        }

        public string Formatted(int spaceIndent, bool canBreak) =>
            m_items.Aggregate(new StringBuilder(), (builder, item) => builder.Append(item.Formatted(spaceIndent, true))).ToString();
    }
}