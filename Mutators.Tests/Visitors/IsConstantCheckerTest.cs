using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators;

using JetBrains.Annotations;

using NUnit.Framework;

namespace Mutators.Tests.Visitors
{
    public class IsConstantCheckerTest : TestBase
    {
        [Test]
        public void EmptyExpression()
        {
            IsConstantExpression(Expression.Lambda<Action>(Expression.Empty(), Enumerable.Empty<ParameterExpression>()));
        }

        [Test]
        public void NullReference()
        {
            IsConstant<object>(() => null);
            IsConstantExpression(Expression.Lambda<Action>(Expression.Constant(null), Enumerable.Empty<ParameterExpression>()));
        }

        [Test]
        public void ConstantValue()
        {
            IsConstant(() => 0);
            IsConstant(() => 1234m);
            IsConstant(() => "GRobas");
        }

        [Test]
        public void NewObject()
        {
            IsConstant(() => new {});
            IsConstant(() => new object());
        }

        [Test]
        public void ParametrizedFunction()
        {
            IsConstant((string x) => "GRobas");
            IsConstant((string x) => x);
            IsConstant((string x) => x + "GRobas");
        }

        [Test]
        public void ExternalParameter()
        {
            Expression<Func<string, Func<string, string>>> exp = x => y => x + y;
            IsConstantExpression(exp);
            IsNotConstantExpression(exp.Body); // 'y => x + y' contains external parameter 'x'
        }

        [Test]
        public void NewGuid()
        {
            IsNotConstant(() => Guid.NewGuid());
            IsNotConstant((object x) => Guid.NewGuid());
        }

        [Test]
        public void TimeNow()
        {
            IsConstant(() => DateTime.Now);
            IsNotConstant(() => DateTime.UtcNow);
            IsNotConstant((object x) => DateTime.UtcNow);
        }

        [Test]
        public void ExplicitlyDynamicFunction()
        {
            IsConstant((Random r) => r.Next());
            IsNotConstant((Random r) => r.Next().Dynamic());
        }

        [Test]
        public void ExplicitlyDynamicValue()
        {
            IsConstant(() => new List<string>());
            IsNotConstant(() => new List<string>().Dynamic());
        }

        private void IsConstant<T>([NotNull] Expression<Func<T>> expression)
        {
            IsConstantExpression(expression);
        }

        private void IsNotConstant<T>([NotNull] Expression<Func<T>> expression)
        {
            IsNotConstantExpression(expression);
        }

        private void IsConstant<TArg, T>([NotNull] Expression<Func<TArg, T>> expression)
        {
            IsConstantExpression(expression);
        }

        private void IsNotConstant<TArg, T>([NotNull] Expression<Func<TArg, T>> expression)
        {
            IsNotConstantExpression(expression);
        }

        private void IsConstantExpression([NotNull] Expression expression)
        {
            Assert.True(expression.IsConstant(), "Expression '{0}' should be constant", expression);
        }

        private void IsNotConstantExpression([NotNull] Expression expression)
        {
            Assert.False(expression.IsConstant(), "Expression '{0}' should not be constant", expression);
        }
    }
}