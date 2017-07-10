using System;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;

using NUnit.Framework;

namespace Compiler.Tests.TryCatchTests
{
    public class FilterExceptionTest : TestBase
    {
        [Test]
        public void TestConstantMessage()
        {
            const string overflowMessage = "Overflow";
            const string invalidCastMessage = "Invalid cast";
            const string nullReferenceMessage = "Null reference";

            ParameterExpression a = Expression.Parameter(typeof(TestClassA), "a");
            ParameterExpression b = Expression.Parameter(typeof(TestClassA), "b");
            TryExpression tryExpr =
                Expression.TryCatchFinally(
                    Expression.Call(
                        Expression.MultiplyChecked(
                            Expression.Convert(Expression.MakeMemberAccess(a, GetMemberInfo((TestClassA x) => x.X)), typeof(int)),
                            Expression.Convert(Expression.MakeMemberAccess(b, GetMemberInfo((TestClassA x) => x.X)), typeof(int))
                        ), typeof(object).GetMethod("ToString")),
                    Expression.Assign(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("B")), Expression.Constant(true)),
                    Expression.Catch(
                        typeof(OverflowException),
                        Expression.Constant(overflowMessage),
                        Expression.Equal(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("F")), Expression.Constant("zzz"))
                    ),
                    Expression.Catch(
                        typeof(InvalidCastException),
                        Expression.Constant(invalidCastMessage),
                        Expression.Equal(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("F")), Expression.Constant("zzz"))
                    ),
                    Expression.Catch(
                        typeof(NullReferenceException),
                        Expression.Constant(nullReferenceMessage),
                        Expression.Equal(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("F")), Expression.Constant("zzz"))
                    )
                );
            var exp = Expression.Lambda<Func<TestClassA, TestClassA, string>>(tryExpr, a, b);

            foreach(var compilerOptions in new[] {CompilerOptions.None, CompilerOptions.All})
            {
                var f = CompileToMethod(exp, compilerOptions);
                CheckNullReferenceException(f, nullReferenceMessage);
                CheckInvalidCastException(f, invalidCastMessage);
                CheckOverflowException(f, overflowMessage);
            }
        }

        [Test]
        public void TestDefaultExceptionMessage()
        {
            ParameterExpression a = Expression.Parameter(typeof(TestClassA), "a");
            ParameterExpression b = Expression.Parameter(typeof(TestClassA), "b");
            ParameterExpression overflow = Expression.Parameter(typeof(OverflowException), "overflow");
            ParameterExpression invalidCast = Expression.Parameter(typeof(InvalidCastException), "invalidCast");
            ParameterExpression nullReference = Expression.Parameter(typeof(NullReferenceException), "nullReference");
            TryExpression tryExpr =
                Expression.TryCatchFinally(
                    Expression.Call(
                        Expression.MultiplyChecked(
                            Expression.Convert(Expression.MakeMemberAccess(a, GetMemberInfo((TestClassA x) => x.X)), typeof(int)),
                            Expression.Convert(Expression.MakeMemberAccess(b, GetMemberInfo((TestClassA x) => x.X)), typeof(int))
                        ), typeof(object).GetMethod("ToString")),
                    Expression.Assign(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("B")), Expression.Constant(true)),
                    Expression.Catch(
                        overflow,
                        Expression.MakeMemberAccess(overflow, GetMemberInfo((Exception x) => x.Message)),
                        Expression.Equal(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("F")), Expression.Constant("zzz"))
                    ),
                    Expression.Catch(
                        invalidCast,
                        Expression.MakeMemberAccess(invalidCast, GetMemberInfo((Exception x) => x.Message)),
                        Expression.Equal(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("F")), Expression.Constant("zzz"))
                    ),
                    Expression.Catch(
                        nullReference,
                        Expression.MakeMemberAccess(nullReference, GetMemberInfo((Exception x) => x.Message)),
                        Expression.Equal(Expression.MakeMemberAccess(null, typeof(FilterExceptionTest).GetField("F")), Expression.Constant("zzz"))
                    )
                );
            var exp = Expression.Lambda<Func<TestClassA, TestClassA, string>>(Expression.Block(new[] {overflow, invalidCast, nullReference}, tryExpr), a, b);

            foreach(var compilerOptions in new[] {CompilerOptions.None, CompilerOptions.All})
            {
                var f = CompileToMethod(exp, compilerOptions);
                CheckNullReferenceException(f, new NullReferenceException().Message);
                CheckInvalidCastException(f, new InvalidCastException().Message);
                CheckOverflowException(f, new OverflowException().Message);
            }
        }

        private static MemberInfo GetMemberInfo<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return ((MemberExpression)expression.Body).Member;
        }

        private static void CheckOverflowException(Func<TestClassA, TestClassA, string> f, string message)
        {
            TestReturns(message, () => f(new TestClassA {X = 1000000}, new TestClassA {X = 1000000}));
            TestThrows<OverflowException>(() => f(new TestClassA {X = 1000000}, new TestClassA {X = 1000000}));
            TestReturns("1000000", () => f(new TestClassA {X = 1000}, new TestClassA {X = 1000}), catchExceptions : false);
        }

        private static void CheckInvalidCastException(Func<TestClassA, TestClassA, string> f, string message)
        {
            TestReturns(message, () => f(new TestClassA {X = "zzz"}, new TestClassA {X = 1}));
            TestReturns(message, () => f(new TestClassA {X = 1}, new TestClassA {X = "zzz"}));

            TestThrows<InvalidCastException>(() => f(new TestClassA {X = "zzz"}, new TestClassA {X = 1}));
            TestThrows<InvalidCastException>(() => f(new TestClassA {X = 1}, new TestClassA {X = "zzz"}));
        }

        private static void CheckNullReferenceException(Func<TestClassA, TestClassA, string> f, string message)
        {
            TestReturns(message, () => f(null, null));
            TestReturns(message, () => f(null, new TestClassA()));
            TestReturns(message, () => f(new TestClassA(), null));
            TestReturns(message, () => f(new TestClassA(), new TestClassA()));
            TestReturns(message, () => f(new TestClassA {X = 1}, new TestClassA()));
            TestReturns(message, () => f(new TestClassA(), new TestClassA {X = 1}));

            TestThrows<NullReferenceException>(() => f(null, null));
            TestThrows<NullReferenceException>(() => f(null, new TestClassA()));
            TestThrows<NullReferenceException>(() => f(new TestClassA(), null));
            TestThrows<NullReferenceException>(() => f(new TestClassA(), new TestClassA()));
            TestThrows<NullReferenceException>(() => f(new TestClassA {X = 1}, new TestClassA()));
            TestThrows<NullReferenceException>(() => f(new TestClassA(), new TestClassA {X = 1}));
        }

        private static void TestThrows<TException>(Action f)
            where TException : Exception
        {
            B = false;
            F = "qxx";
            Assert.Throws<TException>(() => f());
            Assert.IsTrue(B);
        }

        private static void TestReturns(string message, Func<string> f, bool catchExceptions = true)
        {
            B = false;
            F = catchExceptions ? "zzz" : "qxx";
            Assert.AreEqual(message, f());
            Assert.IsTrue(B);
        }

        public static bool B;
        public static string F;

        public class TestClassA
        {
            public object X { get; set; }
            public bool B { get; set; }
            public string Message { get; set; }
        }
    }
}