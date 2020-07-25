// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System.Collections.Immutable;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers
{
    internal interface CToken
    {
        string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak);
    }

    class CBigSpaceToken : CToken
    {
        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CAnySpacesToken : CToken
    {
        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CIdentifierToken : CToken
    {
        public CIdentifierToken(string identifier)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    interface CAssignableToken : CToken
    {}

    class CNamespacedClassNameToken : CAssignableToken
    {
        public CNamespacedClassNameToken(IImmutableList<CIdentifierToken> namespaceAndClassToken)
        {
            (System.Collections.Generic.List<int> A, float[ ][], (int, int)) a = (null, new []{new []{2.0f}}, (2, 3));
        }

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CTemplateContentToken : CToken
    {
        public CTemplateContentToken(IOptional<string> modifier, CAssignableToken namespacedClassNameToken)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CTemplatedClassNameToken : CAssignableToken
    {
        public CTemplatedClassNameToken(CNamespacedClassNameToken identifier, IImmutableList<CTemplateContentToken> templates)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CDeclarableTupleItemToken : CToken
    {
        public CDeclarableTupleItemToken(CAssignableToken assignable, IOptional<CIdentifierToken> identifier)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CAssignableTupleToken: CAssignableToken
    {
        public CAssignableTupleToken(IImmutableList<CDeclarableTupleItemToken> tupleElements)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CAssignableArrayToken : CAssignableToken
    {
        public CAssignableArrayToken(CAssignableToken assignable)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CEqualUsingToken : CToken
    {
        public CEqualUsingToken(CIdentifierToken identifier, CAssignableToken assignableToken)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CUsingToken : CToken
    {
        public CUsingToken(CNamespacedClassNameToken namespacedClassName)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CClassToken : CToken
    {
        CClassToken(IImmutableList<string> modifiers, CIdentifierToken className, IImmutableList<CClassItem> items)
        {}

        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }

    class CFileToken : CToken
    {
        public string Formatted(int previousLineIndent, int charSinceWhiteSpace, bool canBreak) => throw new System.NotImplementedException();
    }
}