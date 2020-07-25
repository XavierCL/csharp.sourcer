// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System;
using System.Collections.Immutable;
using System.Linq;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers
{
    internal static class CLexer
    {
        private static Lazy<IParser<TToken>> LazyParser<TToken>(Func<IParser<TToken>> generator) =>
            new Lazy<IParser<TToken>>(generator);

        private static Lazy<IParser<string>> LazyLiteralParser(string literal) =>
            new Lazy<IParser<string>>(() => new LiteralParser(literal));

        private static Lazy<IParser<string>> LazyRegexParser(string pattern) =>
            new Lazy<IParser<string>>(() => new LiteralParser(pattern));

        private static IParser<CBigSpaceToken> CBigSpace() =>
            new RegexParser(@"\s*\n\s*\n").Map(_ => new CBigSpaceToken());

        private static IParser<CAnySpacesToken> CAnySpaces() =>
            new RegexParser(@"\s+").Map(_ => new CAnySpacesToken());

        private static IParser<IOptional<CAnySpacesToken>> COptionalAnySpaces() => CAnySpaces().Optional();

        private static IParser<CIdentifierToken> CIdentifier() =>
            new RegexParser(@"[a-zA-Z_][a-zA-Z_0-9]*").Map(identifier => new CIdentifierToken(identifier));

        private static IParser<CTemplateContentToken> CTemplateContent() =>
            new RegexParser(@"in|out")
                .Optional()
                .And(LazyParser(() =>
                    COptionalAnySpaces()
                        .AndKeepRight(LazyParser(CAssignable))
                )).Map(modifierAndClassName => new CTemplateContentToken(modifierAndClassName.Left, modifierAndClassName.Right));

        private static IParser<IImmutableList<CTemplateContentToken>> CTemplateList() =>
            new LiteralParser("<")
                .AndKeepRight(LazyParser(COptionalAnySpaces))
                .AndKeepRight(LazyParser(CTemplateContent))
                .And(
                    LazyParser(
                        () =>
                            COptionalAnySpaces()
                                .AndKeepRight(LazyLiteralParser(","))
                                .AndKeepRight(LazyParser(COptionalAnySpaces))
                                .AndKeepRight(LazyParser(CTemplateContent))
                                .Star()
                    )
                )
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .AndKeepLeft(LazyLiteralParser(">"))
                .Map(firstAndRest => firstAndRest.Right.Insert(0, firstAndRest.Left));

        private static IParser<CDeclarableTupleItemToken> CDeclarableTupleItem() =>
            CAssignable()
                .AndKeepLeft(LazyParser(CAnySpaces))
                .And(LazyParser(() => CIdentifier().Optional()))
                .Map(assignableAndIdentifier => new CDeclarableTupleItemToken(assignableAndIdentifier.Left, assignableAndIdentifier.Right));

        private static IParser<CAssignableTupleToken> CAssignableTuple() =>
            new LiteralParser("(")
                .AndKeepRight(LazyParser(COptionalAnySpaces))
                .AndKeepRight(LazyParser(CDeclarableTupleItem))
                .And(
                    LazyParser(
                        () =>
                            COptionalAnySpaces()
                                .AndKeepRight(LazyLiteralParser(","))
                                .AndKeepRight(LazyParser(COptionalAnySpaces))
                                .AndKeepRight(LazyParser(CDeclarableTupleItem))
                                .Star()
                    )
                )
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .AndKeepLeft(LazyLiteralParser(")"))
                .Map(firstTypeAndTheRest => {
                    (CDeclarableTupleItemToken first, IImmutableList<CDeclarableTupleItemToken> rest) = firstTypeAndTheRest;
                    return new CAssignableTupleToken(rest.Insert(0, first));
                });

        private static IParser<CNamespacedClassNameToken> CNamespacedClassName() =>
            CIdentifier()
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .AndKeepLeft(LazyLiteralParser("."))
                .Star()
                .And(LazyParser(CIdentifier))
                .Map(nameSpaceAndClassName => new CNamespacedClassNameToken(nameSpaceAndClassName.Left.Add(nameSpaceAndClassName.Right)));

        private static IParser<CAssignableToken> CAssignable() =>
            CNamespacedClassName()
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .And(LazyParser(() => CTemplateList().Optional()))
                .And(LazyParser(() => COptionalAnySpaces().AndKeepRight(LazyRegexParser(@"\[\s*\]")).Star()))
                .Map(consumedAssignableAndTemplateAndArrays => {
                    ((var namespacedClassName, var optionalTemplates), var arrays) = consumedAssignableAndTemplateAndArrays;

                    CAssignableToken assignable = optionalTemplates.Match<CAssignableToken>(
                        templates => new CTemplatedClassNameToken(namespacedClassName, templates),
                        () => namespacedClassName
                    );

                    return arrays.Aggregate(assignable, (currentAssignable, _) => new CAssignableArrayToken(currentAssignable));
                })
                .Or(LazyParser<CAssignableToken>(CAssignableTuple));

        private static IParser<CEqualUsingToken> CEqualUsing() =>
            new LiteralParser("using")
                .AndKeepRight(LazyParser(CAnySpaces))
                .AndKeepRight(LazyParser(CIdentifier))
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .AndKeepLeft(LazyLiteralParser("="))
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .And(LazyParser(CAssignable))
                .AndKeepLeft(LazyParser(COptionalAnySpaces))
                .AndKeepLeft(LazyLiteralParser(";"))
                .Map(identifierAndAssignable => new CEqualUsingToken(identifierAndAssignable.Left, identifierAndAssignable.Right));

        private static IParser<CUsingToken> CUsing() =>
            new LiteralParser("using")
                .AndKeepRight(LazyParser(CAnySpaces))
                .AndKeepRight(LazyParser(CNamespacedClassName))
                .Map(namespaced => new CUsingToken(namespaced));

        private static IParser<CClassToken> 

        public static IParser<CFileToken> CFile =>
            CBigSpace()
                .Or(LazyParser<CToken>(CAnySpaces))
                .Or(LazyParser<CToken>(CEqualUsing))
                .Or(LazyParser<CToken>(CUsing))
                .Or(LazyParser<CToken>(CClass))
                .Or(LazyParser<CToken>(CInterface))
                .Or(LazyParser<CToken>(CEnum))
                .Or(LazyParser<CToken>(CFunction))
                .Or(LazyParser<CToken>(CSingleLineComment))
                .Or(LazyParser<CToken>(CMultiLineComment))
                .Or(LazyParser<CToken>(CNamespace))
                .Star();
    }
}