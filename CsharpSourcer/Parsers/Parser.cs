// ==========================================================================
// Copyright (C) 2020 by Genetec, Inc.
// All rights reserved.
// May be used only in accordance with a valid Source Code License Agreement.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Genetec.Plugins.Xs.Tools.Collections;

namespace CsharpSourcer.Parsers
{
    public abstract class ParsingException : Exception
    {
        protected ParsingException(string message) : base(message)
        {}

        protected ParsingException(string message, Exception cause) : base(message, cause)
        {}
    }

    public class ProgramParsingException : ParsingException
    {
        internal ProgramParsingException(string message) : base(message)
        {}

        internal ProgramParsingException(string message, Exception cause) : base(message, cause)
        {}
    }

    public class ParsingDefinitionException : ParsingException
    {
        internal ParsingDefinitionException(string message) : base(message)
        {}

        internal ParsingDefinitionException(string message, Exception cause) : base(message, cause)
        {}
    }

    public interface IPartialParse<out OutputType>
    {
        OutputType Output { get; }

        string RemainingInput { get; }
    }

    internal class PartialParse<OutputType> : IPartialParse<OutputType>
    {
        public OutputType Output { get; }

        public string RemainingInput { get; }

        public PartialParse(OutputType output, string remainingInput)
        {
            Output = output;
            RemainingInput = remainingInput;
        }
    }

    public interface IParser<out OutputType>
    {
        IEither<ParsingException, IPartialParse<OutputType>> Parse(string input);

        string Display();

        string DisplayIfNotDisplayed(HashSet<Guid> alreadyPrinted);

        string GetRepresentation(HashSet<Guid> alreadyPrinted);
    }

    public static class ParserExtensions
    {
        public static IParser<OutputType> Named<OutputType>(this IParser<OutputType> parser, string name) => new NamedParser<OutputType>(parser, name);

        public static IParser<OtherOutputType> Map<OutputType, OtherOutputType>
            (this IParser<OutputType> parser, Func<OutputType, OtherOutputType> mappingFunction) =>
            new MapParser<OutputType, OtherOutputType>(parser, mappingFunction);

        public static IParser<(OutputType Left, OtherOutputType Right)> And<OutputType, OtherOutputType>
            (this IParser<OutputType> parser, Lazy<IParser<OtherOutputType>> other) =>
            new AndParser<OutputType, OtherOutputType>(parser, other);

        public static IParser<OutputType> AndKeepLeft<OutputType, OtherOutputType>
            (this IParser<OutputType> parser, Lazy<IParser<OtherOutputType>> other) =>
            parser.And(other).Map(leftAndRight => leftAndRight.Left);

        public static IParser<OtherOutputType> AndKeepRight<OutputType, OtherOutputType>
            (this IParser<OutputType> parser, Lazy<IParser<OtherOutputType>> other) =>
            parser.And(other).Map(leftAndRight => leftAndRight.Right);

        public static IParser<OtherOutputType> Or<OutputType, OtherOutputType>(this IParser<OutputType> parser, Lazy<IParser<OtherOutputType>> other)
            where OutputType: OtherOutputType =>
            new OrParser<OutputType, OtherOutputType>(parser, other);

        public static IParser<IOptional<OutputType>> Optional<OutputType>(this IParser<OutputType> parser) => new OptionalParser<OutputType>(parser);

        public static IParser<IImmutableList<OutputType>> Star<OutputType>(this IParser<OutputType> parser) => new StarParser<OutputType>(parser);

        public static IParser<IImmutableList<OutputType>> Repeat<OutputType>(this IParser<OutputType> parser, int count) =>
            new RepeatParser<OutputType>(parser, count);

        private class NamedParser<OutputType> : Parser<OutputType>
        {
            private readonly IParser<OutputType> m_outer;
            private readonly string m_name;

            public NamedParser(IParser<OutputType> outer, string name)
            {
                m_outer = outer;
                m_name = name;
            }

            public override IEither<ParsingException, IPartialParse<OutputType>> Parse(string input) => m_outer.Parse(input);

            public override string DisplayIfNotDisplayed(HashSet<Guid> alreadyPrinted)
            {
                if (alreadyPrinted.Contains(m_id))
                {
                    return $@"{{""name"": ""{m_name}"", ""oldRef"": ""{m_id}""}}";
                }

                alreadyPrinted.Add(m_id);

                return $@"{{""name"": ""{m_name}"", ""newRef"": ""{m_id}"", ""value"": {m_outer.GetRepresentation(alreadyPrinted)}}}";
            }

            public override string GetRepresentation(HashSet<Guid> alreadyPrinted) => throw new NotImplementedException();
        }

        private class MapParser<OutputType, OtherOutputType> : Parser<OtherOutputType>
        {
            private readonly IParser<OutputType> m_outerParser;
            private readonly Func<OutputType, OtherOutputType> m_mappingFunction;

            public MapParser(IParser<OutputType> outerParser, Func<OutputType, OtherOutputType> mappingFunction)
            {
                m_outerParser = outerParser;
                m_mappingFunction = mappingFunction;
            }

            public override IEither<ParsingException, IPartialParse<OtherOutputType>> Parse(string input) =>
                m_outerParser.Parse(input)
                    .MapRight(partialParse => new PartialParse<OtherOutputType>(m_mappingFunction(partialParse.Output), partialParse.RemainingInput));

            public override string GetRepresentation(HashSet<Guid> alreadyPrinted) => m_outerParser.GetRepresentation(alreadyPrinted);
        }

        private class AndParser<OutputType, OtherOutputType> : Parser<(OutputType, OtherOutputType)>
        {
            private readonly IParser<OutputType> m_first;
            private readonly Lazy<IParser<OtherOutputType>> m_second;

            public AndParser(IParser<OutputType> first, Lazy<IParser<OtherOutputType>> second)
            {
                m_first = first;
                m_second = second;
            }

            public override IEither<ParsingException, IPartialParse<(OutputType, OtherOutputType)>> Parse(string input) =>
                m_first
                    .Parse(input)
                    .Match(
                        Either.Create<IPartialParse<(OutputType, OtherOutputType)>>.Left,
                        partialParse =>
                            m_second
                                .Value
                                .Parse(partialParse.RemainingInput)
                                .MapRight(
                                    secondParse =>
                                        new PartialParse<(OutputType, OtherOutputType)>(
                                            (partialParse.Output, secondParse.Output),
                                            secondParse.RemainingInput
                                        )
                                )
                    )
                    .MapLeft(error => ParsingFailure(input, error));

            public override string GetRepresentation
                (HashSet<Guid> alreadyPrinted) =>
                $@"{{""and"": [{m_first.DisplayIfNotDisplayed(alreadyPrinted)}, ${m_second.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class AndKeepLeftParser<OutputType, OtherOutputType> : Parser<OutputType>
        {
            private readonly IParser<OutputType> m_first;
            private readonly Lazy<IParser<OtherOutputType>> m_second;

            public AndKeepLeftParser(IParser<OutputType> first, Lazy<IParser<OtherOutputType>> second)
            {
                m_first = first;
                m_second = second;
            }

            public override IEither<ParsingException, IPartialParse<OutputType>> Parse(string input) =>
                m_first
                    .Parse(input)
                    .Match(
                        Either.Create<IPartialParse<OutputType>>.Left,
                        partialParse =>
                            m_second
                                .Value
                                .Parse(partialParse.RemainingInput)
                                .MapRight(
                                    secondParse =>
                                        new PartialParse<OutputType>(
                                            partialParse.Output,
                                            secondParse.RemainingInput
                                        )
                                )
                    )
                    .MapLeft(error => ParsingFailure(input, error));

            public override string GetRepresentation
                (HashSet<Guid> alreadyPrinted) =>
                $@"{{""andKeepLeft"": [{m_first.DisplayIfNotDisplayed(alreadyPrinted)}, ${m_second.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class AndKeepRightParser<OutputType, OtherOutputType> : Parser<OtherOutputType>
        {
            private readonly IParser<OutputType> m_first;
            private readonly Lazy<IParser<OtherOutputType>> m_second;

            public AndKeepRightParser(IParser<OutputType> first, Lazy<IParser<OtherOutputType>> second)
            {
                m_first = first;
                m_second = second;
            }

            public override IEither<ParsingException, IPartialParse<OtherOutputType>> Parse(string input) =>
                m_first
                    .Parse(input)
                    .Match(
                        Either.Create<IPartialParse<OtherOutputType>>.Left,
                        partialParse =>
                            m_second
                                .Value
                                .Parse(partialParse.RemainingInput)
                                .MapRight(
                                    secondParse =>
                                        new PartialParse<OtherOutputType>(
                                            secondParse.Output,
                                            secondParse.RemainingInput
                                        )
                                )
                    )
                    .MapLeft(error => ParsingFailure(input, error));

            public override string GetRepresentation
                (HashSet<Guid> alreadyPrinted) =>
                $@"{{""andKeepRight"": [{m_first.DisplayIfNotDisplayed(alreadyPrinted)}, ${m_second.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class OrParser<OutputType, OtherOutputType> : Parser<OtherOutputType>
            where OutputType : OtherOutputType
        {
            private readonly IParser<OutputType> m_outer;
            private readonly Lazy<IParser<OtherOutputType>> m_other;

            public OrParser(IParser<OutputType> outer, Lazy<IParser<OtherOutputType>> other)
            {
                m_outer = outer;
                m_other = other;
            }

            public override IEither<ParsingException, IPartialParse<OtherOutputType>> Parse(string input) =>
                m_outer.Parse(input)
                    .Match(
                        error => m_other.Value.Parse(input)
                            .Match(
                                _ => Either.Create<IPartialParse<OtherOutputType>>.Left(ParsingFailure(input, error)),
                                Either.Create<ParsingException>.Right
                            ),
                        success =>
                            Either.Create<ParsingException>
                                .Right<IPartialParse<OtherOutputType>>(new PartialParse<OtherOutputType>(success.Output, success.RemainingInput))
                    );

            public override string GetRepresentation(HashSet<Guid> alreadyPrinted) =>
                $@"{{""or"": [{m_outer.DisplayIfNotDisplayed(alreadyPrinted)}, {m_other.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class OptionalParser<OutputType> : Parser<IOptional<OutputType>>
        {
            private readonly IParser<OutputType> m_outer;

            public OptionalParser(IParser<OutputType> outer)
            {
                m_outer = outer;
            }

            public override IEither<ParsingException, IPartialParse<IOptional<OutputType>>> Parse(string input) =>
                m_outer.Parse(input)
                    .Match(
                        error => Either.Create<ParsingException>.Right(new PartialParse<IOptional<OutputType>>(Genetec.Plugins.Xs.Tools.Collections.Optional<OutputType>.None, input)),
                        success => Either.Create<ParsingException>.Right(
                            new PartialParse<IOptional<OutputType>>(Genetec.Plugins.Xs.Tools.Collections.Optional.Some(success.Output), success.RemainingInput)
                        )
                    );

            public override string GetRepresentation(HashSet<Guid> alreadyPrinted) => $@"{{""opt"": {m_outer.DisplayIfNotDisplayed(alreadyPrinted)}}}";
        }

        private class StarParser<OutputType> : Parser<IImmutableList<OutputType>>
        {
            private readonly IParser<OutputType> m_outer;

            public StarParser(IParser<OutputType> outer)
            {
                m_outer = outer;
            }

            public override IEither<ParsingException, IPartialParse<IImmutableList<OutputType>>> Parse(string input) =>
                TryPartialParseStarParser(ImmutableList<OutputType>.Empty, input)
                    .MapRight(partialParseAndLastError => partialParseAndLastError.PartialParse);

            public override IEither<Exception, IImmutableList<OutputType>> ParseAll(string input) =>
                TryPartialParseStarParser(ImmutableList<OutputType>.Empty, input)
                    .Match(
                        Either.Create<IImmutableList<OutputType>>.Left,
                        partialParseAndLastError =>
                            partialParseAndLastError.PartialParse.RemainingInput.IsEmpty()
                                ? Either.Create<ParsingException>.Right(partialParseAndLastError.PartialParse.Output)
                                : Either.Create<IImmutableList<OutputType>>.Left(
                                    ParsingAllFailure(input, partialParseAndLastError.PartialParse.RemainingInput, partialParseAndLastError.LastError)
                                )
                    );

            public override string GetRepresentation(HashSet<Guid> alreadyPrinted) => $@"{{""star"": {m_outer.DisplayIfNotDisplayed(alreadyPrinted)}}}";

            private IEither<ParsingDefinitionException, (IPartialParse<IImmutableList<OutputType>> PartialParse, ParsingException LastError)>
                TryPartialParseStarParser
                (
                    IImmutableList<OutputType> outputs,
                    string remainingInput
                ) =>
                m_outer.Parse(remainingInput)
                    .Match(
                        error =>
                            Either.Create<ParsingDefinitionException>
                                .Right<(IPartialParse<IImmutableList<OutputType>>, ParsingException)>(
                                    (new PartialParse<IImmutableList<OutputType>>(outputs, remainingInput), error)
                                ),
                        success => success.RemainingInput == remainingInput
                            ? Either.Create<(IPartialParse<IImmutableList<OutputType>>, ParsingException)>.Left(
                                new ParsingDefinitionException($"Infinite match while parsing {remainingInput} using {Display()}")
                            )
                            : TryPartialParseStarParser(outputs.Add(success.Output), success.RemainingInput)
                    );
        }

        private class RepeatParser<OutputType> : Parser<IImmutableList<OutputType>>
        {
            private readonly IParser<OutputType> m_outer;
            private readonly int m_count;

            public RepeatParser(IParser<OutputType> outer, int count)
            {
                m_outer = outer;
                m_count = count;
            }

            public override IEither<ParsingException, IPartialParse<IImmutableList<OutputType>>> Parse(string input) =>
                RepeatParse(m_count, input, ImmutableList<OutputType>.Empty);

            public override string GetRepresentation(HashSet<Guid> alreadyPrinted) =>
                $@"{{""repeat"": {{""count"": {m_count}, ""inner"": {m_outer.DisplayIfNotDisplayed(alreadyPrinted)}}}}}";

            private IEither<ParsingException, IPartialParse<IImmutableList<OutputType>>> RepeatParse
            (
                int currentCount,
                string remainingInput,
                IImmutableList<OutputType> outputs
            )
            {
                if (currentCount <= 0) return Either.Create<ParsingException>.Right(new PartialParse<IImmutableList<OutputType>>(outputs, remainingInput));

                return m_outer.Parse(remainingInput)
                    .Match(
                        error => Either.Create<IPartialParse<IImmutableList<OutputType>>>.Left(ParsingFailure(remainingInput, error)),
                        success => RepeatParse(currentCount - 1, success.RemainingInput, outputs.Add(success.Output))
                    );
            }
        }
    }

    public abstract class Parser<OutputType> : IParser<OutputType>
    {
        protected readonly Guid m_id = Guid.NewGuid();

        public abstract IEither<ParsingException, IPartialParse<OutputType>> Parse(string input);

        public virtual string DisplayIfNotDisplayed(HashSet<Guid> alreadyPrinted)
        {
            if (alreadyPrinted.Contains(m_id))
            {
                return $@"{{""oldRef"": ""{m_id}""}}";
            }

            alreadyPrinted.Add(m_id);

            return $@"{{""newRef"": ""{m_id}"", ""value"": {GetRepresentation(alreadyPrinted)}}}";
        }

        public string Display() => DisplayIfNotDisplayed(new HashSet<Guid>());

        public abstract string GetRepresentation(HashSet<Guid> alreadyPrinted);

        public virtual IEither<Exception, OutputType> ParseAll(string input) =>
            Parse(input)
                .Match(
                    Either.Create<OutputType>.Left,
                    partialParse =>
                        partialParse.RemainingInput.IsEmpty()
                            ? Either.Create<Exception>.Right(partialParse.Output)
                            : Either.Create<OutputType>.Left<Exception>(ParsingAllFailure(input, partialParse.RemainingInput))
                );

        protected ProgramParsingException ParsingFailure(string input, Exception cause = null) =>
            new ProgramParsingException($@"Could not parse ""{input}"" using {Display()}", cause);

        protected ProgramParsingException ParsingAllFailure(string input, string remainingInput, Exception cause = null) =>
            new ProgramParsingException($@"""{remainingInput}"" remained when parsing ""{input}"" using {Display()}", cause);
    }
}