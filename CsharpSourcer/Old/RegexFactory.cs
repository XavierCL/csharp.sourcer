// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Text.RegularExpressions;

namespace CsharpSourcer.Parsers
{
    internal class RegexFactory
    {
        internal static Regex R(string content) => new Regex(content, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
    }
}