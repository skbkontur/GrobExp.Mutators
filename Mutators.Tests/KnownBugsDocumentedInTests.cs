using System;

using FluentAssertions;

using GrobExp.Mutators;

using Mutators.Tests.Helpers;

using NUnit.Framework;

namespace Mutators.Tests
{
    [TestFixture]
    public class KnownBugsDocumentedInTests
    {
        [Test(Description = "Mutators cannot deal with different source arrays for single destination path")]
        public void TestConvertWithDifferentArraySources()
        {
            var collection = new TestConverterCollection<AValue, BValue>(new PathFormatterCollection(), configurator =>
                {
                    configurator.If(a => a.Condition)
                                .Target(b => b.BOuterArray.Each().BInnerArray.Each().FirstValue)
                                .Set(a => a.AOuterArray.Current().AFirstAFirstInnerArray.Current().Value);
                    configurator.If(a => !a.Condition)
                                .Target(b => b.BOuterArray.Each().BInnerArray.Each().FirstValue)
                                .Set(a => a.AOuterArray.Current().ASecondASecondInnerArray.Current().Value);
                });
            var converter = collection.GetConverter(MutatorsContext.Empty);
            var from = new AValue
                {
                    AOuterArray = new[]
                        {
                            new AOuterValue
                                {
                                    AFirstAFirstInnerArray = new[]
                                        {
                                            new AFirstInnerValue {Value = "123"}
                                        },
                                    ASecondASecondInnerArray = new[]
                                        {
                                            new ASecondInnerValue {Value = "321"}
                                        },
                                }
                        },
                    Condition = true,
                };
            Following.Code(() => converter(from))
                     .Should().Throw<InvalidOperationException>()
                     .Which.Message.Should().MatchRegex(@"^Method T Current\[T\].* cannot be invoked$");

            return;

            var expected = new BValue
                {
                    BOuterArray = new[]
                        {
                            new BOuterValue
                                {
                                    BInnerArray = new[]
                                        {
                                            new BInnerValue{FirstValue = "123"},
                                        },
                                },
                        },
                };
            converter(from).Should().BeEquivalentTo(expected);
        }

        [Test(Description = "Mutators cannot deal with different source arrays for single destination array")]
        public void TestSeveralArraysToOne()
        {
            var collection = new TestConverterCollection<AValue, BValue>(new PathFormatterCollection(), configurator =>
                {
                    configurator.GoTo(x => x.BOuterArray.Each(), x => x.AOuterArray.Current())
                                .GoTo(x => x.BInnerArray.Each(), x => x.AFirstAFirstInnerArray.Current())
                                .Target(x => x.SecondValue).Set(x => x.Value);

                    configurator.GoTo(x => x.BOuterArray.Each(), x => x.AOuterArray.Current())
                                .GoTo(x => x.BInnerArray.Each(), x => x.ASecondASecondInnerArray.Current())
                                .Target(x => x.FirstValue).Set(x => x.Value);
                });
            var converter = collection.GetConverter(MutatorsContext.Empty);
            var from = new AValue
                {
                    AOuterArray = new[]
                        {
                            new AOuterValue
                                {
                                    AFirstAFirstInnerArray = new[]
                                        {
                                            new AFirstInnerValue {Value = "123"}
                                        },
                                    ASecondASecondInnerArray = new[]
                                        {
                                            new ASecondInnerValue {Value = "321"}
                                        },
                                }
                        }
                };
            Following.Code(() => converter(from))
                     .Should().Throw<InvalidOperationException>()
                     .Which.Message.Should().MatchRegex(@"^Method T Current\[T\].* cannot be invoked$");

            return;

            var expected = new BValue
                {
                    BOuterArray = new[]
                        {
                            new BOuterValue
                                {
                                    BInnerArray = new[]
                                        {
                                            new BInnerValue
                                                {
                                                    SecondValue = "123",
                                                    FirstValue = "321"
                                                },
                                        }
                                }
                        }
                };

            converter(from).Should().BeEquivalentTo(expected);
        }

        private class AValue
        {
            public AOuterValue[] AOuterArray { get; set; }

            public bool Condition { get; set; }
        }

        private class AOuterValue
        {
            public AFirstInnerValue[] AFirstAFirstInnerArray { get; set; }
            public ASecondInnerValue[] ASecondASecondInnerArray { get; set; }
        }

        private class ASecondInnerValue
        {
            public string Value { get; set; }
        }

        private class AFirstInnerValue
        {
            public string Value { get; set; }
        }

        private class BValue
        {
            public BOuterValue[] BOuterArray { get; set; }
        }

        private class BOuterValue
        {
            public BInnerValue[] BInnerArray { get; set; }
        }

        private class BInnerValue
        {
            public string FirstValue { get; set; }
            public string SecondValue { get; set; }
        }
    }
}