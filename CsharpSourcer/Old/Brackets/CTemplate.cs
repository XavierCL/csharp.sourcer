// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers.Brackets
{
    internal class CTemplate : CItem
    {
        private static readonly Regex TEMPLATE_START = RegexFactory.R(@"\s*<");
        private static readonly Regex NEXT_TEMPLATE = RegexFactory.R(@"([^<>]*)(<|>)");
        private static readonly Regex SPACES = RegexFactory.R(@"\s*");
        private static readonly Regex SPACED_COMMA = RegexFactory.R(@"\s*,\s*");

        private readonly ImmutableList<CItem> m_innerTypes;

        public int CharEndIndexExcluded { get; }

        private CTemplate(ImmutableList<CItem> innerTypes, int charEndIndexExcluded)
        {
            m_innerTypes = innerTypes;
            CharEndIndexExcluded = charEndIndexExcluded;
        }

        public string Formatted(int previousLineIndent, int spaceIndent, int charSinceWhiteSpace, bool canBreak)
        {
            if (!canBreak)
            {
                return m_innerTypes.Aggregate(
                        new StringBuilder($"{CFile.Indent(spaceIndent)}<"),
                        (builder, item) => {
                            if (item is CAny any)
                            {
                                return builder.Append(SPACES.Replace(SPACED_COMMA.Replace(any.Formatted(0, false), ", "), " ").Trim());
                            }

                            return builder.Append(item.Formatted(previousLineIndent, 0, builder.ToString().Length + charSinceWhiteSpace, false));
                        }
                    )
                    .Append(">")
                    .ToString();
            }

            string formattedWithoutBreak = Formatted(previousLineIndent, spaceIndent, charSinceWhiteSpace, false);

            if (formattedWithoutBreak.Length + charSinceWhiteSpace > CFile.HARD_LINE_BREAK)
            {
                return m_innerTypes.Aggregate(
                        new StringBuilder(CFile.LINE_ENDING).Append(CFile.Indent(previousLineIndent)).Append("<").Append(CFile.LINE_ENDING),
                        (builder, item) => {
                            if (item is CAny any)
                            {
                                string[] commaSeparated = SPACES.Replace(SPACED_COMMA.Replace(any.Formatted(0, false), ","), " ").Trim().Split(",");

                                commaSeparated.Take(commaSeparated.Length - 1)
                                    .ForEach(
                                        separated => builder.Append(CFile.Indent(previousLineIndent + 1))
                                            .Append(separated)
                                            .Append(",")
                                            .Append(CFile.LINE_ENDING)
                                    );

                                commaSeparated.Skip(commaSeparated.Length - 1)
                                    .ForEach(
                                        separated => builder.Append(CFile.Indent(previousLineIndent + 1)).Append(separated).Append(CFile.LINE_ENDING)
                                    );

                                return builder;
                            }

                            return builder
                                .Append(item.Formatted(previousLineIndent + 1, 0, builder.ToString().Split("\n").LastOrDefault()?.Length ?? 0, true))
                                .Append(CFile.LINE_ENDING);
                        }
                    )
                    .Append(CFile.Indent(previousLineIndent))
                    .Append(">")
                    .Append(CFile.LINE_ENDING)
                    .ToString();
            }

            return formattedWithoutBreak;
        }

        public static IOptional<CTemplate> Match(string content, int startIndex)
        {
            Match match = TEMPLATE_START.Match(content, startIndex);

            if (!match.Success) return Optional<CTemplate>.None;

            var items = new List<CItem>();

            Match nextTemplateMatch = NEXT_TEMPLATE.Match(content, match.Index + match.Length);

            while (nextTemplateMatch.Success
                && nextTemplateMatch.Groups[2].Value != ">")
            {
                IOptional<CTemplate> cInnerTemplate = Match(content, nextTemplateMatch.Groups[1].Index + nextTemplateMatch.Groups[1].Length);

                Match templateMatch = nextTemplateMatch;

                bool OnInnerTemplate(CTemplate innerTemplate)
                {
                    items.Add(new CAny(templateMatch.Groups[1].Value, templateMatch.Groups[1].Index + templateMatch.Groups[1].Length));
                    items.Add(innerTemplate);
                    nextTemplateMatch = NEXT_TEMPLATE.Match(content, innerTemplate.CharEndIndexExcluded);
                    return true;
                }

                static bool NoTemplate() => false;

                if (!cInnerTemplate.Match(OnInnerTemplate, NoTemplate)) return Optional<CTemplate>.None;
            }

            if (!nextTemplateMatch.Success) return Optional<CTemplate>.None;

            return Optional.Some(new CTemplate(items.ToImmutableList(), nextTemplateMatch.Index + nextTemplateMatch.Length));
        }
    }
}