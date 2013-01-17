using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestTry
    {
        [Test]
        public void Test1()
        {
            TryExpression tryCatchExpr =
                Expression.TryCatch(
                    Expression.Block(
                        Expression.Throw(Expression.Constant(new DivideByZeroException())),
                        Expression.Constant("Try block")
                        ),
                    Expression.Catch(
                        typeof(DivideByZeroException),
                        Expression.Constant("Catch block")
                        )
                    );
            var exp = Expression.Lambda<Func<string>>(tryCatchExpr);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual("Catch block", f());
        }
    }
}