using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    public class ExpressionNullCheckingExtenderTest : TestBase
    {
        #region Setup/Teardown

        protected override void SetUp()
        {
            expressionNullCheckingExtender = new ExpressionNullCheckingExtender();
            base.SetUp();
        }

        #endregion

        [Test]
        public void Test1()
        {
            Expression<Func<TestClassA, string>> exp = o => o.S;
            Expression<Func<TestClassA, string>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, string> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {S = "zzz"}), Is.EqualTo("zzz"));
        }

        [Test]
        public void Test1x()
        {
            Expression<Func<TestClassA, int>> exp = o => o.Y;
            Expression<Func<TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA {Y = 1}), Is.EqualTo(1));
        }

        [Test, Ignore("Bug")]
        public void Test1z()
        {
            var intsExp = (Expression<Func<int, int[]>>)(i => new int[i]);
            MemberInfo intsLength = ((MemberExpression)intsExp.Body).Member;
            MemberInfo stringsLength = ((MemberExpression)((Expression<Func<string[], int>>)(arr => arr.Length)).Body).Member;
            Console.WriteLine(intsLength == stringsLength);
            Expression<Func<TestClassA, string>> exp = o => o.S;
            var zexp = Expression.Lambda<Action<TestClassA>>(Expression.Assign(exp.Body, Expression.Convert(expressionNullCheckingExtender.Extend(exp.Body), typeof(string))), exp.Parameters);
            //var zexp = Expression.Lambda<Action<TestClassA>>(Expression.Assign(exp.Body, expressionNullCheckingExtender.Extend(exp.Body)), exp.Parameters);
            //var zexp = Expression.Lambda<Action<TestClassA>>(Expression.Assign(exp.Body, Expression.Convert(Expression.Convert(expressionNullCheckingExtender.Extend(exp.Body), typeof(object)), typeof(string))), exp.Parameters);
            var f = zexp.Compile();
            f(new TestClassA());
            Expression<Func<TestClassA, string>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, string> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {S = "zzz"}), Is.EqualTo("zzz"));
        }

        [Test]
        public void TestArrayLength()
        {
            Expression<Func<TestClassA, int>> exp = a => a.ArrayB.Length;
            var visitedExp = ExtendNullChecking(exp);
            var f = visitedExp.Compile();
            Assert.AreEqual(0, f(null));
            Assert.AreEqual(0, f(new TestClassA()));
            Assert.AreEqual(1, f(new TestClassA {ArrayB = new TestClassB[1]}));
        }

        [Test]
        public void TestSelectMany()
        {
            //sg26.SG34.SelectMany(sg34 => sg34.MonetaryAmount, (sg34, amount) => new {sg34, amount}).Where(@t => @t.sg34.DutyTaxFeeDetails.DutyTaxFeeFunctionCodeQualifier == DutyTaxFeeFunctionCodeQualifier.Tax
            //&& @t.sg34.DutyTaxFeeDetails.DutyTaxFeeType.DutyTaxFeeTypeNameCode == DutyTaxFeeTypeNameCode.VAT
            //&& @t.amount.MonetaryAmountGroup.MonetaryAmountTypeCodeQualifier == MonetaryAmountTypeCodeQualifier.TaxAmount).Select(@t => @t.amount.MonetaryAmountGroup.MonetaryAmount)).FirstOrDefault()

            Expression<Func<TestClassA, string>> exp = a => (a.ArrayB.SelectMany(b => b.C.ArrayD, (b, d) => new {b, d}).Where(@t => @t.d.Z == "zzz").Select(@t => @t.d.E.S)).FirstOrDefault();
            Expression<Func<TestClassA, string>> visitedExp = ExtendNullChecking(exp);
            var f = visitedExp.Compile();
            Assert.IsNull(f(null));
            Assert.IsNull(f(new TestClassA()));
            Assert.IsNull(f(new TestClassA {ArrayB = new TestClassB[0]}));

            Assert.IsNull(f(new TestClassA {ArrayB = new[] {new TestClassB()}}));
            Assert.IsNull(f(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC()}}}));
            Assert.IsNull(f(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new TestClassD[0]}}}}));
            Assert.IsNull(f(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD {Z = "zzz"}}}}}}));
            Assert.IsNull(f(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD {Z = "zzz", E = new TestClassE()}}}}}}));
            Assert.AreEqual("qxx", f(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD {Z = "zzz", E = new TestClassE {S = "qxx"}}}}}}}));
        }

        [Test]
        public void TestToStringOfGuid()
        {
            Expression<Func<TestClassA, string>> exp = f => "_xxx_" + f.Guid.ToString();
            Expression<Func<TestClassA, string>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, string> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(new TestClassA { Guid = new Guid("8DCBF7DF-772A-4A9C-81F0-D4B25C183ACE") }), Is.EqualTo("_xxx_8DCBF7DF-772A-4A9C-81F0-D4B25C183ACE").IgnoreCase);
        }

//        [Test]
//        public void Test10()
//        {
//            Expression<Func<TestClassA, Currency>> exp = o => o.Z;
//            Expression<Func<TestClassA, Currency>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, Currency> compiledVisitedExp = visitedExp.Compile();
//            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
//            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1) }), Is.EqualTo(new Currency(1)));
//        }
//
//        [Test]
//        public void Test11()
//        {
//            Expression<Func<TestClassA, Currency>> exp = o => o.B.Z + o.Z;
//            Expression<Func<TestClassA, Currency>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, Currency> compiledVisitedExp = visitedExp.Compile();
//            Assert.That(compiledVisitedExp(null), Is.EqualTo(Currency.Zero));
//            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(Currency.Zero));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1) }), Is.EqualTo(new Currency(1)));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1), B = new TestClassB() }), Is.EqualTo(new Currency(1)));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(new Currency(2)));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1), B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(new Currency(3)));
//        }
//
//        [Test]
//        public void Test12()
//        {
//            Expression<Func<TestClassA, bool>> exp = o => o.B.Z < o.Z;
//            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
//            Assert.That(compiledVisitedExp(null), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1) }), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1), B = new TestClassB() }), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1), B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(false));
//        }
//
//        [Test]
//        public void Test13()
//        {
//            Expression<Func<TestClassA, bool>> exp = o => o.B.Z == null;
//            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
//            Assert.That(compiledVisitedExp(null), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB() }), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(false));
//        }
//
//        [Test]
//        public void Test14()
//        {
//            Expression<Func<TestClassA, bool>> exp = o => o.B.Z != null;
//            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
//            Assert.That(compiledVisitedExp(null), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB() }), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(true));
//        }
//
//        [Test]
//        public void Test15()
//        {
//            Expression<Func<TestClassA, bool>> exp = o => o.B.Z == o.Z;
//            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
//            Assert.That(compiledVisitedExp(null), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1) }), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1), B = new TestClassB() }), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB() }), Is.EqualTo(true));
//            Assert.That(compiledVisitedExp(new TestClassA { B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(1), B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(false));
//            Assert.That(compiledVisitedExp(new TestClassA { Z = new Currency(2), B = new TestClassB { Z = new Currency(2) } }), Is.EqualTo(true));
//        }

        [Test]
        public void Test2()
        {
            Expression<Func<TestClassA, string>> exp = o => o.B.S;
            Expression<Func<TestClassA, string>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, string> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {B = new TestClassB {S = "zzz"}}), Is.EqualTo("zzz"));
        }

        [Test]
        public void Test3()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.B.X;
            Expression<Func<TestClassA, int?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int?> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {B = new TestClassB {X = 1}}), Is.EqualTo(1));
        }

        [Test]
        public void Test4()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.B.F2( /*new Qzz{X = 1}*/1);
            Expression<Func<TestClassA, int?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int?> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {B = new TestClassB {X = 1}}), Is.EqualTo(1));
        }

        [Test]
        public void Test5()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].X;
            Expression<Func<TestClassA, int?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int?> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new TestClassB[] {null}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB()}}), Is.EqualTo(null));
            int? actual = compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {X = 1}}});
            Assert.That(actual, Is.EqualTo(1));
        }

        [Test]
        public void Test5x()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB[0].C.ArrayD[1].X;
            Expression<Func<TestClassA, int?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int?> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new TestClassB[] {null}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB()}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC()}}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new TestClassD[0]}}}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD(),}}}}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD(), null,}}}}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD(), new TestClassD(),}}}}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {C = new TestClassC {ArrayD = new[] {new TestClassD(), new TestClassD{X = 1},}}}}}), Is.EqualTo(1));
        }

        [Test]
        public void Test6()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.ArrayB.Sum(b => b.X);
            Expression<Func<TestClassA, int?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int?> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB()}}), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {X = 2}}}), Is.EqualTo(2));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {X = 2}, null}}), Is.EqualTo(2));
        }

        [Test]
        public void Test7a()
        {
            Expression<Func<TestClassA, int>> exp = o => o.B.Y + o.Y;
            Expression<Func<TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA {Y = 1}), Is.EqualTo(1));
            Assert.That(compiledVisitedExp(new TestClassA {Y = 1, B = new TestClassB()}), Is.EqualTo(1));
            Assert.That(compiledVisitedExp(new TestClassA {B = new TestClassB {Y = 2}}), Is.EqualTo(2));
            Assert.That(compiledVisitedExp(new TestClassA {Y = 1, B = new TestClassB {Y = 2}}), Is.EqualTo(3));
        }

        [Test]
        public void Test7b()
        {
            Expression<Func<TestClassA, int?>> exp = o => o.B.X + o.X;
            Expression<Func<TestClassA, int?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int?> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {X = 1}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {X = 1, B = new TestClassB()}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {B = new TestClassB {X = 2}}), Is.EqualTo(null));
            Assert.That(compiledVisitedExp(new TestClassA {X = 1, B = new TestClassB {X = 2}}), Is.EqualTo(3));
        }

        [Test]
        public void Test8()
        {
            Expression<Func<TestClassA, int>> exp = o => o.ArrayB.Sum(b => b.Y);
            Expression<Func<TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB()}}), Is.EqualTo(0));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {Y = 2}}}), Is.EqualTo(2));
            Assert.That(compiledVisitedExp(new TestClassA {ArrayB = new[] {new TestClassB {Y = 2}, null}}), Is.EqualTo(2));
        }

        [Test]
        public void Test9()
        {
            Expression<Func<TestClassA, int>> exp = o => (o.B ?? new TestClassB {Y = 3}).Y;
            Expression<Func<TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.EqualTo(3));
            Assert.That(compiledVisitedExp(new TestClassA()), Is.EqualTo(3));
            Assert.That(compiledVisitedExp(new TestClassA {B = new TestClassB {Y = 2}}), Is.EqualTo(2));
        }

        [Test]
        public void Test23()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.NullableBool == false;
            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
            Assert.IsFalse(compiledVisitedExp(null));
            Assert.IsFalse(compiledVisitedExp(new TestClassA()));
        }

        [Test]
        public void TestLogical1()
        {
            Expression<Func<TestClassA, bool?>> exp = o => o.NullableBool;
            Expression<Func<TestClassA, bool?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool?> compiledVisitedExp = visitedExp.Compile();
            Assert.IsNull(compiledVisitedExp(null));
            Assert.IsNull(compiledVisitedExp(new TestClassA()));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {NullableBool = true}));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {NullableBool = false}));
        }

        [Test]
        public void TestLogical2()
        {
            Expression<Func<TestClassA, bool?>> exp = o => !o.NullableBool;
            Expression<Func<TestClassA, bool?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool?> compiledVisitedExp = visitedExp.Compile();
            Assert.IsNull(compiledVisitedExp(null));
            Assert.IsNull(compiledVisitedExp(new TestClassA()));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {NullableBool = true}));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {NullableBool = false}));
        }

        [Test]
        public void TestLogical3()
        {
            Expression<Func<TestClassA, bool?>> exp = o => o.B.X > 0;
            Expression<Func<TestClassA, bool?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool?> compiledVisitedExp = visitedExp.Compile();
            Assert.IsNull(compiledVisitedExp(null));
            Assert.IsNull(compiledVisitedExp(new TestClassA()));
            Assert.IsNull(compiledVisitedExp(new TestClassA {B = new TestClassB()}));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {B = new TestClassB {X = -1}}));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {B = new TestClassB {X = 1}}));
        }

        [Test]
        public void TestLogical4()
        {
            Expression<Func<TestClassA, bool?>> exp = o => !(o.B.X > 0);
            Expression<Func<TestClassA, bool?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool?> compiledVisitedExp = visitedExp.Compile();
            Assert.IsNull(compiledVisitedExp(null));
            Assert.IsNull(compiledVisitedExp(new TestClassA()));
            Assert.IsNull(compiledVisitedExp(new TestClassA {B = new TestClassB()}));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {B = new TestClassB {X = -1}}));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {B = new TestClassB {X = 1}}));
        }

        [Test]
        public void TestLogical5()
        {
            Expression<Func<TestClassA, bool?>> exp = o => o.B.X > 0 && o.A.X > 0;
            Expression<Func<TestClassA, bool?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool?> compiledVisitedExp = visitedExp.Compile();
            Assert.IsNull(compiledVisitedExp(null));
            Assert.IsNull(compiledVisitedExp(new TestClassA()));
            Assert.IsNull(compiledVisitedExp(new TestClassA {B = new TestClassB()}));
            Assert.IsNull(compiledVisitedExp(new TestClassA {B = new TestClassB {X = 1}}));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {B = new TestClassB {X = -1}}));
            Assert.IsNull(compiledVisitedExp(new TestClassA {A = new TestClassA {X = 1}}));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {A = new TestClassA {X = -1}}));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {A = new TestClassA {X = 1}, B = new TestClassB {X = 1}}));
        }

        [Test]
        public void TestLogical6()
        {
            Expression<Func<TestClassA, bool?>> exp = o => o.B.X > 0 || o.A.X > 0;
            Expression<Func<TestClassA, bool?>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool?> compiledVisitedExp = visitedExp.Compile();
            Assert.IsNull(compiledVisitedExp(null));
            Assert.IsNull(compiledVisitedExp(new TestClassA()));
            Assert.IsNull(compiledVisitedExp(new TestClassA {B = new TestClassB()}));
            Assert.IsNull(compiledVisitedExp(new TestClassA {B = new TestClassB {X = -1}}));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {B = new TestClassB {X = 1}}));
            Assert.IsNull(compiledVisitedExp(new TestClassA {A = new TestClassA {X = -1}}));
            Assert.AreEqual(true, compiledVisitedExp(new TestClassA {A = new TestClassA {X = 1}}));
            Assert.AreEqual(false, compiledVisitedExp(new TestClassA {A = new TestClassA {X = -1}, B = new TestClassB {X = -1}}));
        }

        [Test]
        public void TestNullableGuidNoCoalesce()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.NullableGuid == null;
            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
            Assert.IsFalse(compiledVisitedExp(new TestClassA {NullableGuid = Guid.Empty}));
        }

//        [Test]
//        public void TestExpressionCompilationBug()
//        {
//            Expression<Func<TestClassA, Currency>> exp = o => new Currency(110) + Currency.Min(new Currency(210), o.Z);
//            Expression<Func<TestClassA, Currency>> visitedExp = ExtendNullChecking(exp);
//            Func<TestClassA, Currency> compiledVisitedExp = visitedExp.Compile();
//            Assert.AreEqual(new Currency(120), compiledVisitedExp(new TestClassA { Z = new Currency(10) }));
//        }

        [Test]
        public void TestLazyEvaluation()
        {
            Expression<Func<TestClassA, bool>> exp = o => o.A != null && Y(o.A) > 0;
            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.False);
            Assert.That(compiledVisitedExp(new TestClassA()), Is.False);
            Assert.That(compiledVisitedExp(new TestClassA {A = new TestClassA()}), Is.False);
            Assert.That(compiledVisitedExp(new TestClassA {A = new TestClassA {Y = 1}}), Is.True);
        }

        [Test]
        public void TestLazyEvaluation2()
        {
            Expression<Func<int?, bool>> exp = o => o != null && o.ToString() == "1";
            Expression<Func<int?, bool>> visitedExp = ExtendNullChecking(exp);
            Func<int?, bool> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(null), Is.False);
            Assert.That(compiledVisitedExp(1), Is.True);
        }

        [Test]
        public void TestStaticMethod()
        {
            Expression<Func<TestClassA, TestClassA, int>> exp = (x, y) => NotExtension(x, y);
            Expression<Func<TestClassA, TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, TestClassA, int> compiledVisitedExp = visitedExp.Compile();

            Assert.That(compiledVisitedExp(new TestClassA(), new TestClassA()), Is.EqualTo(3), "!null,!null");
            Assert.That(compiledVisitedExp(new TestClassA(), null), Is.EqualTo(2), "!null,null");
            Assert.That(compiledVisitedExp(null, null), Is.EqualTo(1), "null,null");
        }

        [Test]
        public void TestStaticMethodAsSubChain()
        {
            Expression<Func<TestClassA, TestClassA, int>> exp = (x, y) => NotExtension2(x.A, y.A).Y;
            Expression<Func<TestClassA, TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, TestClassA, int> compiledVisitedExp = visitedExp.Compile();

            Assert.That(compiledVisitedExp(new TestClassA {A = new TestClassA()}, new TestClassA {A = new TestClassA()}), Is.EqualTo(3), "!null,!null");
            Assert.That(compiledVisitedExp(new TestClassA {A = new TestClassA()}, null), Is.EqualTo(2), "!null,null");
            Assert.That(compiledVisitedExp(null, null), Is.EqualTo(1), "null,null");
        }

        [Test]
        public void TestCompileExtendedExpressionWithClosure()
        {
            var closure = 11;
            Expression<Func<TestClassA, bool>> exp = o => closure == o.Y;
            Expression<Func<TestClassA, bool>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, bool> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(new TestClassA {Y = 11}), Is.True);
        }

        [Test]
        public void TestNullableValidType()
        {
            Expression<Func<int?, string>> exp = o => o == null ? "" : o.ToString();
            Expression<Func<int?, string>> visitedExp = ExtendNullChecking(exp);
            Func<int?, string> compiledVisitedExp = visitedExp.Compile();
            Assert.AreEqual(compiledVisitedExp(null), "");
        }

        [Test]
        public void TestExtendFunctionArguments()
        {
            Expression<Func<TestClassA, int>> exp = o => zzz(o.X > 0);
            Expression<Func<TestClassA, int>> visitedExp = ExtendNullChecking(exp);
            Func<TestClassA, int> compiledVisitedExp = visitedExp.Compile();
            Assert.AreEqual(0, compiledVisitedExp(null));
            Assert.AreEqual(0, compiledVisitedExp(new TestClassA()));
            Assert.AreEqual(1, compiledVisitedExp(new TestClassA {X = 1}));
        }

        [Ignore("Баг во фреймворке?")]
        [Test]
        public void TestBUG()
        {
            Expression<Func<int[], bool>> exp = o => 11 == o[o.Count() - 1];
            Expression<Func<int[], bool>> visitedExp = ExtendNullChecking(exp);
            Func<int[], bool> compiledVisitedExp = visitedExp.Compile();
            Assert.That(compiledVisitedExp(new[] {1, 11}), Is.True);
        }

        [Test]
        public void TestExtendForNullableValueTypes()
        {
            Expression<Func<int?, string>> exp = s => s.ToString();
            Expression<Func<int?, string>> visitedExp = ExtendNullChecking(exp);
            Func<int?, string> compiledVisitedExp = visitedExp.Compile();
            Assert.AreEqual(compiledVisitedExp(null), null);
            Assert.AreEqual(compiledVisitedExp(1), "1");
        }

        [Test]
        public void TestExtendForNullableValueTypes2()
        {
            Expression<Func<int?, bool>> exp = s => s == 0;
            Expression<Func<int?, bool>> visitedExp = ExtendNullChecking(exp);
            Func<int?, bool> compiledVisitedExp = visitedExp.Compile();
            Assert.IsFalse(compiledVisitedExp(null));
        }

        [Test]
        public void TestBadArrayIndex()
        {
            Expression<Func<TestClassA, int>> exp = a => a.IntArray[314159265];
            Expression<Func<TestClassA, int>> extendedExp = ExtendNullChecking(exp);
            var compiledExtendedExp = extendedExp.Compile();
            Assert.AreEqual(0, compiledExtendedExp(new TestClassA {IntArray = new[] {1, 2, 3}}));
        }

        [Test]
        public void TestBadArrayIndex2()
        {
#pragma warning disable 251
            Expression<Func<TestClassA, int>> exp = a => a.IntArray[-1];
#pragma warning restore 251
            Expression<Func<TestClassA, int>> extendedExp = ExtendNullChecking(exp);
            var compiledExtendedExp = extendedExp.Compile();
            Assert.AreEqual(0, compiledExtendedExp(new TestClassA {IntArray = new[] {1, 2, 3}}));
        }

        [Test]
        public void TestBadArrayIndex3()
        {
            Expression<Func<TestClassA, string>> exp = a => a.ArrayB[271828183].C.D.E.S;
            Expression<Func<TestClassA, string>> extendedExp = ExtendNullChecking(exp);
            var compiledExtendedExp = extendedExp.Compile();
            Assert.AreEqual(null, compiledExtendedExp(new TestClassA {ArrayB = new[] {new TestClassB()}}));
        }

        private int zzz(bool qxx)
        {
            return qxx ? 1 : 0;
        }

        private static int NotExtension(TestClassA x, TestClassA y)
        {
            if(x == null) return 1;
            if(y == null) return 2;
            return 3;
        }

        private static TestClassA NotExtension2(TestClassA x, TestClassA y)
        {
            if(x == null) return new TestClassA {Y = 1};
            if(y == null) return new TestClassA {Y = 2};
            return new TestClassA {Y = 3};
        }

        private static int Y(TestClassA a)
        {
            return a.Y;
        }

        private Expression<TDelegate> ExtendNullChecking<TDelegate>(Expression<TDelegate> exp)
        {
            return (Expression<TDelegate>)expressionNullCheckingExtender.Extend(exp);
        }

        private ExpressionNullCheckingExtender expressionNullCheckingExtender;

        private class TestClassA
        {
            public string S { get; set; }
            public TestClassA A { get; set; }
            public TestClassB B { get; set; }
            public TestClassB[] ArrayB { get; set; }
            public int[] IntArray { get; set; }
            public int? X;
            public Guid Guid = Guid.Empty;
            public Guid? NullableGuid;
            public bool? NullableBool;
            public int Y;
        }

        private class TestClassB
        {
            public int? F2(int? x)
            {
                return x;
            }

            public int? F( /*Qzz*/ int a, int b)
            {
                return b;
            }

            public string S { get; set; }

            public TestClassC C { get; set; }
            public int? X;
            public int Y;
        }

        private class TestClassC
        {
            public string S { get; set; }

            public TestClassD D { get; set; }

            public TestClassD[] ArrayD { get; set; }
        }

        private class TestClassD
        {
            public TestClassE E { get; set; }
            public string Z { get; set; }

            public int? X { get; set; }

            public readonly string S;
        }

        private class TestClassE
        {
            public string S { get; set; }
        }

        private struct Qzz
        {
            public long X;
        }
    }
}