// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace CsharpSourcer.Parsers
{
    internal class CFile
    {
        private const string INDENT = "    ";
        private readonly ImmutableList<CItem> m_items;

        public static string LINE_ENDING { get; } = "\r\n";

        public static int HARD_LINE_BREAK { get; } = 140;

        internal CFile(string fileContent)
        {
            int charIndex = 0;
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

        public string Formatted() => m_items.Aggregate(new StringBuilder(), (builder, item) => builder.Append(item.Formatted(0, false))).ToString();

        internal static string Indent(int indent) => string.Concat(Enumerable.Repeat(INDENT, indent));
    }
}