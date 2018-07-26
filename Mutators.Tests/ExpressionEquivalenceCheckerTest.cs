using System;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    [TestFixture]
    public class ExpressionEquivalenceCheckerTest
    {
        [Test]
        public void TestParameter()
        {
            var parameter = Expression.Parameter(typeof(string));

            TestEquivalent(parameter, parameter);

            var otherParameterOfSameType = Expression.Parameter(parameter.Type);

            TestEquivalent(parameter, otherParameterOfSameType, strictly : false);
            TestNotEquivalent(parameter, otherParameterOfSameType, strictly : true);
            TestEquivalent(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"), strictly : false);

            TestNotEquivalent(parameter, Expression.Parameter(typeof(int)));
        }

        [Test]
        public void TestConstant()
        {
            TestEquivalentConstants(null, null);

            var constant = new object();

            TestEquivalentConstants(constant, constant);
            TestNotEquivalentConstants(constant, null);

            TestNotEquivalentConstants(constant, new object());

            TestEquivalentConstants(new ObjectWithEqualsMethod {Field = 4}, new ObjectWithEqualsMethod {Field = 4});
            TestNotEquivalentConstants(new ObjectWithEqualsMethod {Field = 8}, new ObjectWithEqualsMethod {Field = 15});

            TestEquivalentConstants(new[] {constant, new ObjectWithEqualsMethod {Field = 16}, null}, new[] {constant, new ObjectWithEqualsMethod {Field = 16}, null});
            TestNotEquivalentConstants(new[] {constant, new ObjectWithEqualsMethod {Field = 23}, null}, new[] {constant, new ObjectWithEqualsMethod {Field = 23}});
            TestNotEquivalentConstants(new[] {constant, new ObjectWithEqualsMethod {Field = 42}, null}, new[] {constant, new ObjectWithEqualsMethod {Field = 5}, null});
        }

        [Test]
        public void TestUnaryConvert()
        {
            void DoTestUnaryConvert(Func<Expression, Type, UnaryExpression> unaryConvertOperation)
            {
                var parameter = Expression.Parameter(typeof(B));
                TestEquivalent(unaryConvertOperation(parameter, typeof(A)), unaryConvertOperation(parameter, typeof(A)));
                TestNotEquivalent(unaryConvertOperation(parameter, typeof(object)), unaryConvertOperation(parameter, typeof(A)));
                TestNotEquivalent(unaryConvertOperation(parameter, typeof(object)), unaryConvertOperation(Expression.Constant(new B()), typeof(object)));

                var intParameter = Expression.Parameter(typeof(int));
                TestNotEquivalent(unaryConvertOperation(intParameter, typeof(object)), Expression.Negate(intParameter));
            }

            DoTestUnaryConvert(Expression.Convert);
            DoTestUnaryConvert(Expression.ConvertChecked);
            DoTestUnaryConvert(Expression.TypeAs);
        }

        [Test]
        public void TestUnaryArrayLength()
        {
            var array = Expression.Parameter(typeof(int[]));
            TestEquivalent(Expression.ArrayLength(array), Expression.ArrayLength(array));
            TestNotEquivalent(Expression.ArrayLength(array), Expression.ArrayLength(Expression.Parameter(typeof(object[]))));
            TestNotEquivalent(Expression.ArrayLength(array), Expression.TypeAs(array, typeof(object[])));
        }

        [Test]
        public void TestUnaryArithmetic()
        {
            TestUnaryExpressions(Expression.Parameter(typeof(int)), Expression.Parameter(typeof(long)),
                                 Expression.Negate,
                                 Expression.NegateChecked,
                                 Expression.UnaryPlus,
                                 Expression.Increment,
                                 Expression.PreIncrementAssign,
                                 Expression.PostIncrementAssign,
                                 Expression.Decrement,
                                 Expression.PreDecrementAssign,
                                 Expression.PostDecrementAssign,
                                 Expression.OnesComplement
                );
        }

        [Test]
        public void TestUnaryLogical()
        {
            TestUnaryExpressions(Expression.Parameter(typeof(bool)), Expression.Not(Expression.Parameter(typeof(bool))),
                                 Expression.IsTrue,
                                 Expression.IsFalse,
                                 Expression.Not
                );
        }

        private void TestUnaryExpressions(Expression parameter, Expression otherParameter, params Func<Expression, UnaryExpression>[] unaryOperations)
        {
            foreach (var unaryArithmeticOperation in unaryOperations)
            {
                TestEquivalent(unaryArithmeticOperation(parameter), unaryArithmeticOperation(parameter));
                TestNotEquivalent(unaryArithmeticOperation(parameter), unaryArithmeticOperation(otherParameter));
            }
            for (var i = 0; i < unaryOperations.Length; ++i)
            {
                for (var j = 0; j < i; ++j)
                {
                    TestNotEquivalent(unaryOperations[i](parameter), unaryOperations[j](parameter));
                }
            }
        }

        [Test]
        public void TestBinaryArithmetic()
        {
            var firstParameter = Expression.Parameter(typeof(double));
            var secondParameter = Expression.Parameter(typeof(double));

            TestBinaryOperations(firstParameter, secondParameter,
                                 Expression.Add,
                                 Expression.AddChecked,
                                 Expression.AddAssign,
                                 Expression.AddAssignChecked,
                                 Expression.Divide,
                                 Expression.DivideAssign,
                                 Expression.Modulo,
                                 Expression.ModuloAssign,
                                 Expression.Multiply,
                                 Expression.MultiplyChecked,
                                 Expression.MultiplyAssign,
                                 Expression.MultiplyAssignChecked,
                                 Expression.Power,
                                 Expression.PowerAssign,
                                 Expression.Subtract,
                                 Expression.SubtractChecked,
                                 Expression.SubtractAssign,
                                 Expression.SubtractAssignChecked
                );
        }

        [Test]
        public void TestBinaryBitOperations()
        {
            var firstParameter = Expression.Parameter(typeof(int));
            var secondParameter = Expression.Parameter(typeof(int));

            TestBinaryOperations(firstParameter, secondParameter,
                                 Expression.And,
                                 Expression.AndAssign,
                                 Expression.Or,
                                 Expression.OrAssign,
                                 Expression.ExclusiveOr,
                                 Expression.ExclusiveOrAssign,
                                 Expression.LeftShift,
                                 Expression.LeftShiftAssign,
                                 Expression.RightShift,
                                 Expression.RightShiftAssign
                );
        }

        [Test]
        public void TestBinaryComparisonOperations()
        {
            var firstParameter = Expression.Parameter(typeof(int));
            var secondParameter = Expression.Parameter(typeof(int));

            TestBinaryOperations(firstParameter, secondParameter,
                                 Expression.Equal,
                                 Expression.NotEqual,
                                 Expression.GreaterThan,
                                 Expression.GreaterThanOrEqual,
                                 Expression.LessThan,
                                 Expression.LessThanOrEqual
                );
        }

        [Test]
        public void TestBinaryConditionalBooleanOperations()
        {
            var firstParameter = Expression.Parameter(typeof(bool));
            var secondParameter = Expression.Parameter(typeof(bool));

            TestBinaryOperations(firstParameter, secondParameter,
                                 Expression.AndAlso,
                                 Expression.OrElse
                );
        }

        [Test]
        public void TestBinaryAssignOperation()
        {
            TestBinaryOperations(Expression.Parameter(typeof(int)), Expression.Parameter(typeof(int)), Expression.Assign);
        }

        [Test]
        public void TestBinaryArrayIndex()
        {
            var array = Expression.Parameter(typeof(string[]));
            var index = Expression.Parameter(typeof(int));

            TestEquivalent(Expression.ArrayIndex(array, index), Expression.ArrayIndex(array, index));
            TestNotEquivalent(Expression.ArrayIndex(Expression.Parameter(typeof(int[])), index), Expression.ArrayIndex(array, index));
            TestNotEquivalent(Expression.ArrayIndex(array, Expression.Constant(0)), Expression.ArrayIndex(array, index));
        }

        [Test]
        public void TestBinaryWithExplicitMethod()
        {
            var firstObject = Expression.Parameter(typeof(ObjectWithEqualityOperator));
            var secondObject = Expression.Parameter(typeof(ObjectWithEqualityOperator));

            var methodInfo1 = typeof(ObjectWithEqualityOperator).GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static);
            var methodInfo2 = typeof(ObjectWithEqualityOperator).GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static);
            TestNotEquivalent(Expression.Equal(firstObject, secondObject, false, methodInfo1), Expression.Equal(firstObject, secondObject, false, methodInfo2));
            TestEquivalent(Expression.Equal(firstObject, secondObject, false, methodInfo1), Expression.Equal(firstObject, secondObject));
        }

        [Test]
        public void TestBinaryCoalesce()
        {
            var firstParameter = Expression.Parameter(typeof(int?));
            var secondParameter = Expression.Parameter(typeof(int));

            TestEquivalent(Expression.Coalesce(firstParameter, secondParameter), Expression.Coalesce(firstParameter, secondParameter));
            TestNotEquivalent(Expression.Coalesce(firstParameter, Expression.Constant(0)), Expression.Coalesce(firstParameter, secondParameter));
            TestNotEquivalent(Expression.Coalesce(Expression.Constant(null, typeof(int?)), secondParameter), Expression.Coalesce(firstParameter, secondParameter));
        }

        [Test]
        public void TestBinaryCoalesceWithConversion()
        {
            var conversionParameter = Expression.Parameter(typeof(int));
            var complexConversion = Expression.Lambda(Expression.Convert(Expression.Add(conversionParameter, Expression.Constant(5, typeof(int))), typeof(long)), conversionParameter);

            var parameter = Expression.Parameter(typeof(int?));
            var binaryExpression = Expression.Coalesce(parameter, Expression.Constant(92L, typeof(long)), complexConversion);

            TestEquivalent(binaryExpression, binaryExpression);
            TestNotEquivalent(binaryExpression, Expression.Coalesce(parameter, Expression.Constant(92L, typeof(long))));

            var simpleConversion = Expression.Lambda(Expression.Convert(conversionParameter, typeof(long)), conversionParameter);
            TestNotEquivalent(binaryExpression, Expression.Coalesce(parameter, Expression.Constant(92L, typeof(long)), simpleConversion));
        }

        [Test]
        public void TestMethodCall()
        {
            var instance = Expression.Parameter(typeof(string));
            var parameter = Expression.Parameter(typeof(string));

            var containsMethodInfo = typeof(string).GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(containsMethodInfo, Is.Not.Null);

            TestEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(instance, containsMethodInfo, parameter));
            TestNotEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(instance, containsMethodInfo, instance), strictly : true);
            TestNotEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(parameter, containsMethodInfo, parameter), strictly : true);

            var endsWithMethodInfo = typeof(string).GetMethod("EndsWith", new[] {typeof(string)});
            Assert.That(endsWithMethodInfo, Is.Not.Null);

            TestNotEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(instance, endsWithMethodInfo, parameter));
        }

        [Test]
        public void TestEachAndCurrentMethodCall()
        {
            var instance = Expression.Parameter(typeof(int[]));

            TestEquivalent(instance.MakeEachCall(), instance.MakeEachCall());
            TestEquivalent(instance.MakeCurrentCall(), instance.MakeCurrentCall());
            TestNotEquivalent(instance.MakeEachCall(), instance.MakeCurrentCall(), distinguishEachAndCurrent : true);
            TestEquivalent(instance.MakeEachCall(), instance.MakeCurrentCall(), distinguishEachAndCurrent : false);
        }

        [Test]
        public void TestConditional()
        {
            var condition = Expression.Parameter(typeof(bool));
            var ifTrue = Expression.Parameter(typeof(string));
            var ifFalse = Expression.Parameter(typeof(string));

            TestEquivalent(Expression.Condition(condition, ifTrue, ifFalse), Expression.Condition(condition, ifTrue, ifFalse));
            TestNotEquivalent(Expression.Condition(Expression.Constant(true), ifTrue, ifFalse), Expression.Condition(condition, ifTrue, ifFalse));
            TestNotEquivalent(Expression.Condition(condition, Expression.Constant("zzz"), ifFalse), Expression.Condition(condition, ifTrue, ifFalse));
            TestNotEquivalent(Expression.Condition(condition, ifTrue, Expression.Constant("qxx")), Expression.Condition(condition, ifTrue, ifFalse));
        }

        private void TestBinaryOperations(Expression firstParameter, Expression secondParameter, params Func<Expression, Expression, BinaryExpression>[] binaryOperations)
        {
            foreach (var binaryOperation in binaryOperations)
            {
                TestEquivalent(binaryOperation(firstParameter, secondParameter), binaryOperation(firstParameter, secondParameter));
                TestNotEquivalent(binaryOperation(secondParameter, firstParameter), binaryOperation(firstParameter, secondParameter), strictly : true);
                TestNotEquivalent(binaryOperation(firstParameter, firstParameter), binaryOperation(firstParameter, secondParameter), strictly : true);
                TestNotEquivalent(binaryOperation(secondParameter, secondParameter), binaryOperation(firstParameter, secondParameter), strictly : true);
            }

            for (var i = 0; i < binaryOperations.Length; ++i)
            {
                for (var j = 0; j < i; ++j)
                {
                    TestNotEquivalent(binaryOperations[i](firstParameter, secondParameter), binaryOperations[j](firstParameter, secondParameter));
                }
            }
        }

        private static void TestEquivalentConstants(object first, object second, bool strictly = true, bool distinguishEachAndCurrent = true)
        {
            TestEquivalent(Expression.Constant(first), Expression.Constant(second), strictly, distinguishEachAndCurrent);
        }

        private static void TestNotEquivalentConstants(object first, object second, bool strictly = false, bool distinguishEachAndCurrent = false)
        {
            TestNotEquivalent(Expression.Constant(first), Expression.Constant(second), strictly, distinguishEachAndCurrent);
        }

        private static void TestEquivalent(Expression first, Expression second, bool strictly = true, bool distinguishEachAndCurrent = true)
        {
            Assert.That(ExpressionEquivalenceChecker.Equivalent(first, second, strictly, distinguishEachAndCurrent), Is.True);
            Assert.That(ExpressionEquivalenceChecker.Equivalent(second, first, strictly, distinguishEachAndCurrent), Is.True);
        }

        private static void TestNotEquivalent(Expression first, Expression second, bool strictly = false, bool distinguishEachAndCurrent = false)
        {
            Assert.That(ExpressionEquivalenceChecker.Equivalent(first, second, strictly, distinguishEachAndCurrent), Is.False);
            Assert.That(ExpressionEquivalenceChecker.Equivalent(second, first, strictly, distinguishEachAndCurrent), Is.False);
        }

        private class ObjectWithEqualsMethod
        {
            public int Field { get; set; }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null))
                    return false;
                return obj is ObjectWithEqualsMethod other && Field == other.Field;
            }
        }

        private class ObjectWithEqualityOperator
        {
            public int Field { get; set; }

            public static bool operator ==(ObjectWithEqualityOperator first, ObjectWithEqualityOperator second)
            {
                return first.Field == second.Field;
            }

            public static bool operator !=(ObjectWithEqualityOperator first, ObjectWithEqualityOperator second)
            {
                return !(first == second);
            }
        }

        private class A
        {
        }

        private class B : A
        {
        }
    }
}