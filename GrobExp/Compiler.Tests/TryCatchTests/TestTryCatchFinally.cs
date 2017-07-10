using System;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Compiler;

using NUnit.Framework;

namespace Compiler.Tests.TryCatchTests
{
    public class TestTryCatchFinally : TestBase
    {
        [Test]
        public void TestTryCatch()
        {
            TryExpression tryCatchExpr =
                Expression.TryCatch(
                    Expression.Block(
                        Expression.Throw(Expression.New(typeof(DivideByZeroException))),
                        Expression.Constant("Try block")
                    ),
                    Expression.Catch(
                        typeof(DivideByZeroException),
                        Expression.Constant("Catch block")
                    )
                );
            var exp = Expression.Lambda<Func<string>>(tryCatchExpr);
            var f = CompileToMethod(exp, CompilerOptions.All);
            Assert.AreEqual("Catch block", f());
        }

        [Test]
        public void TestTryCatchFinallyConstantMessage()
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
                    Expression.Assign(Expression.MakeMemberAccess(null, typeof(TestTryCatchFinally).GetField("B")), Expression.Constant(true)),
                    Expression.Catch(
                        typeof(OverflowException),
                        Expression.Constant(overflowMessage)
                    ),
                    Expression.Catch(
                        typeof(InvalidCastException),
                        Expression.Constant(invalidCastMessage)
                    ),
                    Expression.Catch(
                        typeof(NullReferenceException),
                        Expression.Constant(nullReferenceMessage)
                    )
                );
            var exp = Expression.Lambda<Func<TestClassA, TestClassA, string>>(tryExpr, a, b);
            foreach(var compilerOptions in new[] {CompilerOptions.None, CompilerOptions.All})
            {
                var f = Compile(exp, compilerOptions);
                CheckNullReferenceException(f, nullReferenceMessage);
                CheckInvalidCastException(f, invalidCastMessage);
                CheckOverflowException(f, overflowMessage);
            }
        }

        [Test]
        public void TestTryCatchFinallyDefaultExceptionMessage()
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
                    Expression.Assign(Expression.MakeMemberAccess(null, typeof(TestTryCatchFinally).GetField("B")), Expression.Constant(true)),
                    Expression.Catch(
                        overflow,
                        Expression.MakeMemberAccess(overflow, GetMemberInfo((Exception x) => x.Message))
                    ),
                    Expression.Catch(
                        invalidCast,
                        Expression.MakeMemberAccess(invalidCast, GetMemberInfo((Exception x) => x.Message))
                    ),
                    Expression.Catch(
                        nullReference,
                        Expression.MakeMemberAccess(nullReference, GetMemberInfo((Exception x) => x.Message))
                    )
                );
            var exp = Expression.Lambda<Func<TestClassA, TestClassA, string>>(Expression.Block(new[] {overflow, invalidCast, nullReference}, tryExpr), a, b);
            foreach(var compilerOptions in new[] {CompilerOptions.None, CompilerOptions.All})
            {
                var f = Compile(exp, compilerOptions);
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
            TestReturns("1000000", () => f(new TestClassA {X = 1000}, new TestClassA {X = 1000}));
        }

        private static void CheckInvalidCastException(Func<TestClassA, TestClassA, string> f, string message)
        {
            TestReturns(message, () => f(new TestClassA {X = "zzz"}, new TestClassA {X = 1}));
            TestReturns(message, () => f(new TestClassA {X = 1}, new TestClassA {X = "zzz"}));
        }

        private static void CheckNullReferenceException(Func<TestClassA, TestClassA, string> f, string message)
        {
            TestReturns(message, () => f(null, null));
            TestReturns(message, () => f(null, new TestClassA()));
            TestReturns(message, () => f(new TestClassA(), null));
            TestReturns(message, () => f(new TestClassA(), new TestClassA()));
            TestReturns(message, () => f(new TestClassA {X = 1}, new TestClassA()));
            TestReturns(message, () => f(new TestClassA(), new TestClassA {X = 1}));
        }

        private static void TestReturns(string message, Func<string> f)
        {
            B = false;
            Assert.AreEqual(message, f());
            Assert.IsTrue(B);
        }

        public static bool B;

        public class TestClassA
        {
            public object X { get; set; }
            public bool B { get; set; }
            public string Message { get; set; }
        }
    }
}