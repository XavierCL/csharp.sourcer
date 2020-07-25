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

        string DisplayIfNotDisplayed(HashSet<string> alreadyPrinted);
    }

    public abstract class Parser<OutputType> : IParser<OutputType>
    {
        protected virtual string Id => Guid.NewGuid().ToString();

        public abstract IEither<ParsingException, IPartialParse<OutputType>> Parse(string input);

        public string DisplayIfNotDisplayed(HashSet<string> alreadyPrinted)
        {
            if (alreadyPrinted.Contains(Id))
            {
                return $@"{{""oldRef"": ""{Id}""}}";
            }

            alreadyPrinted.Add(Id);

            return $@"{{""newRef"": ""{Id}"", ""value"": {GetRepresentation(alreadyPrinted)}}}";
        }

        public string Display() => DisplayIfNotDisplayed(new HashSet<string>());

        public virtual IEither<Exception, OutputType> ParseAll(string input) =>
            Parse(input)
                .Match(
                    Either.Create<OutputType>.Left,
                    partialParse =>
                        partialParse.RemainingInput.IsEmpty()
                            ? Either.Create<Exception>.Right(partialParse.Output)
                            : Either.Create<OutputType>.Left<Exception>(ParsingAllFailure(input, partialParse.RemainingInput))
                );

        public IParser<OtherOutputType> Map<OtherOutputType>(Func<OutputType, OtherOutputType> mappingFunction) =>
            new MapParser<OtherOutputType>(this, mappingFunction);

        public IParser<(OutputType, OtherOutputType)> And<OtherOutputType>(Lazy<IParser<OtherOutputType>> other) =>
            new AndParser<OtherOutputType>(this, other);

        public IParser<OutputType> AndKeepLeft<OtherOutputType>(Lazy<IParser<OtherOutputType>> other) =>
            new AndKeepLeftParser<OtherOutputType>(this, other);

        public IParser<OtherOutputType> AndKeepRight<OtherOutputType>(Lazy<IParser<OtherOutputType>> other) =>
            new AndKeepRightParser<OtherOutputType>(this, other);

        public IParser<OutputType> Or(Lazy<IParser<OutputType>> other) => new OrParser(this, other);

        public IParser<IOptional<OutputType>> Opt() => new OptionalParser(this);

        public IParser<IImmutableList<OutputType>> Star() => new StarParser(this);

        public IParser<IImmutableList<OutputType>> Repeat(int count) => new RepeatParser(this, count);

        protected abstract string GetRepresentation(HashSet<string> alreadyPrinted);

        protected ProgramParsingException ParsingFailure(string input, Exception cause = null) =>
            new ProgramParsingException($@"Could not parse ""{input}"" using {Display()}", cause);

        protected ProgramParsingException ParsingAllFailure(string input, string remainingInput, Exception cause = null) =>
            new ProgramParsingException($@"""{remainingInput}"" remained when parsing ""{input}"" using {Display()}", cause);

        private class NamedParser : Parser<OutputType>
        {
            private readonly Parser<OutputType> m_outer;

            protected override string Id { get; }

            public NamedParser(Parser<OutputType> outer, string name)
            {
                m_outer = outer;
                Id = name;
            }

            public override IEither<ParsingException, IPartialParse<OutputType>> Parse(string input) => m_outer.Parse(input);

            protected override string GetRepresentation(HashSet<string> alreadyPrinted) => m_outer.GetRepresentation(alreadyPrinted);
        }

        private class MapParser<OtherOutputType> : Parser<OtherOutputType>
        {
            private readonly Parser<OutputType> m_outerParser;
            private readonly Func<OutputType, OtherOutputType> m_mappingFunction;

            public MapParser(Parser<OutputType> outerParser, Func<OutputType, OtherOutputType> mappingFunction)
            {
                m_outerParser = outerParser;
                m_mappingFunction = mappingFunction;
            }

            public override IEither<ParsingException, IPartialParse<OtherOutputType>> Parse(string input) =>
                m_outerParser.Parse(input)
                    .MapRight(partialParse => new PartialParse<OtherOutputType>(m_mappingFunction(partialParse.Output), partialParse.RemainingInput));

            protected override string GetRepresentation(HashSet<string> alreadyPrinted) => m_outerParser.GetRepresentation(alreadyPrinted);
        }

        private class AndParser<OtherOutputType> : Parser<(OutputType, OtherOutputType)>
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

            protected override string GetRepresentation
                (HashSet<string> alreadyPrinted) =>
                $@"{{""and"": [{m_first.DisplayIfNotDisplayed(alreadyPrinted)}, ${m_second.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class AndKeepLeftParser<OtherOutputType> : Parser<OutputType>
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

            protected override string GetRepresentation
                (HashSet<string> alreadyPrinted) =>
                $@"{{""andKeepLeft"": [{m_first.DisplayIfNotDisplayed(alreadyPrinted)}, ${m_second.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class AndKeepRightParser<OtherOutputType> : Parser<OtherOutputType>
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

            protected override string GetRepresentation
                (HashSet<string> alreadyPrinted) =>
                $@"{{""andKeepRight"": [{m_first.DisplayIfNotDisplayed(alreadyPrinted)}, ${m_second.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class OrParser : Parser<OutputType>
        {
            private readonly IParser<OutputType> m_outer;
            private readonly Lazy<IParser<OutputType>> m_other;

            public OrParser(IParser<OutputType> outer, Lazy<IParser<OutputType>> other)
            {
                m_outer = outer;
                m_other = other;
            }

            public override IEither<ParsingException, IPartialParse<OutputType>> Parse(string input) =>
                m_outer.Parse(input)
                    .Match(
                        error => m_other.Value.Parse(input)
                            .Match(
                                _ => Either.Create<IPartialParse<OutputType>>.Left(ParsingFailure(input, error)),
                                Either.Create<ParsingException>.Right
                            ),
                        Either.Create<ParsingException>.Right
                    );

            protected override string GetRepresentation(HashSet<string> alreadyPrinted) =>
                $@"{{""or"": [{m_outer.DisplayIfNotDisplayed(alreadyPrinted)}, {m_other.Value.DisplayIfNotDisplayed(alreadyPrinted)}]}}";
        }

        private class OptionalParser : Parser<IOptional<OutputType>>
        {
            private readonly IParser<OutputType> m_outer;

            public OptionalParser(IParser<OutputType> outer)
            {
                m_outer = outer;
            }

            public override IEither<ParsingException, IPartialParse<IOptional<OutputType>>> Parse(string input) =>
                m_outer.Parse(input)
                    .Match(
                        error => Either.Create<ParsingException>.Right(new PartialParse<IOptional<OutputType>>(Optional<OutputType>.None, input)),
                        success => Either.Create<ParsingException>.Right(
                            new PartialParse<IOptional<OutputType>>(Optional.Some(success.Output), success.RemainingInput)
                        )
                    );

            protected override string GetRepresentation(HashSet<string> alreadyPrinted) =>
                $@"{{""opt"": {m_outer.DisplayIfNotDisplayed(alreadyPrinted)}}}";
        }

        private class StarParser : Parser<IImmutableList<OutputType>>
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

            protected override string GetRepresentation(HashSet<string> alreadyPrinted) =>
                $@"{{""star"": {m_outer.DisplayIfNotDisplayed(alreadyPrinted)}}}";

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

        private class RepeatParser : Parser<IImmutableList<OutputType>>
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

            protected override string GetRepresentation(HashSet<string> alreadyPrinted) =>
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
}