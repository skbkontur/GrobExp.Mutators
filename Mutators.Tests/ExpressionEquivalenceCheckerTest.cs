using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using GrobExp.Mutators;
using GrobExp.Mutators.Visitors;

using NUnit.Framework;

namespace Mutators.Tests
{
    [Parallelizable(ParallelScope.All)]
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
                var parameter = Expression.Parameter(typeof(SubclassWithProperty));
                TestEquivalent(unaryConvertOperation(parameter, typeof(ClassWithProperties)), unaryConvertOperation(parameter, typeof(ClassWithProperties)));
                TestNotEquivalent(unaryConvertOperation(parameter, typeof(object)), unaryConvertOperation(parameter, typeof(SubclassWithProperty)));
                TestNotEquivalent(unaryConvertOperation(parameter, typeof(object)), unaryConvertOperation(Expression.Constant(new SubclassWithProperty()), typeof(object)));

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

        [Test]
        public void TestUnaryUnbox()
        {
            var parameter = Expression.Parameter(typeof(object));
            TestEquivalent(Expression.Unbox(parameter, typeof(int)), Expression.Unbox(parameter, typeof(int)));
            TestNotEquivalent(Expression.Unbox(parameter, typeof(int)), Expression.Unbox(parameter, typeof(long)));
            TestNotEquivalent(Expression.Unbox(parameter, typeof(int)), Expression.Unbox(Expression.Constant(2, typeof(object)), typeof(int)));
        }

        [Test]
        public void TestUnaryQuote()
        {
            var parameter = Expression.Parameter(typeof(int));
            var firstLambda = Expression.Lambda(parameter, parameter);
            var secondLambda = Expression.Lambda(Expression.Add(parameter, Expression.Constant(2)), parameter);
            TestEquivalent(Expression.Quote(firstLambda), Expression.Quote(firstLambda));
            TestNotEquivalent(Expression.Quote(firstLambda), Expression.Quote(secondLambda));
        }

        [Test]
        public void TestUnaryThrow()
        {
            var parameter = Expression.Constant(new Exception());
            TestEquivalent(Expression.Throw(parameter), Expression.Throw(parameter));
            TestNotEquivalent(Expression.Throw(parameter), Expression.Throw(Expression.Parameter(typeof(Expression))));
        }

        private void TestUnaryExpressions(Expression parameter, Expression otherParameter, params Func<Expression, UnaryExpression>[] unaryOperations)
        {
            foreach (var unaryArithmeticOperation in unaryOperations)
            {
                TestEquivalent(unaryArithmeticOperation(parameter), unaryArithmeticOperation(parameter));
                TestNotEquivalent(unaryArithmeticOperation(parameter), unaryArithmeticOperation(otherParameter));
            }

            foreach (var (first, second) in AllPairs(unaryOperations))
            {
                TestNotEquivalent(first(parameter), second(parameter));
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

            var containsMethodInfo = typeof(string).GetMethod("Contains", new[] {typeof(string)});
            Assert.That(containsMethodInfo, Is.Not.Null);

            TestEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(instance, containsMethodInfo, parameter));
            TestNotEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(instance, containsMethodInfo, instance), strictly : true);
            TestNotEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(parameter, containsMethodInfo, parameter), strictly : true);

            var endsWithMethodInfo = typeof(string).GetMethod("EndsWith", new[] {typeof(string)});
            Assert.That(endsWithMethodInfo, Is.Not.Null);

            TestNotEquivalent(Expression.Call(instance, containsMethodInfo, parameter), Expression.Call(instance, endsWithMethodInfo, parameter));
        }

        [Test]
        public void TestMethodCallKeepsParameterMappings()
        {
            var firstParameter = Expression.Parameter(typeof(ClassWithDictionary));
            var secondParameter = Expression.Parameter(typeof(ClassWithDictionary));

            var dictProperty = typeof(ClassWithDictionary).GetProperty("Dict", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(dictProperty, Is.Not.Null);

            var getItemMethod = typeof(Dictionary<string, int>).GetMethod("get_Item");
            Assert.That(getItemMethod, Is.Not.Null);

            var firstLambda = Expression.Lambda(Expression.Call(Expression.MakeMemberAccess(firstParameter, dictProperty), getItemMethod, Expression.Constant("abc")), firstParameter);
            var secondLambda = Expression.Lambda(Expression.Call(Expression.MakeMemberAccess(secondParameter, dictProperty), getItemMethod, Expression.Constant("abc")), secondParameter);

            TestEquivalent(firstLambda, firstLambda);
            TestEquivalent(firstLambda, secondLambda);
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

        [Test]
        public void TestBlock()
        {
            var firstVariable = Expression.Variable(typeof(int));
            var secondVariable = Expression.Variable(typeof(object));

            var parameter = Expression.Parameter(typeof(int));

            TestEquivalent(Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[] {parameter}),
                           Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[] {parameter}));

            TestEquivalent(Expression.Block(typeof(void), variables : null, expressions : new Expression[] {parameter}),
                           Expression.Block(typeof(void), variables : null, expressions : new Expression[] {parameter}));

            TestEquivalent(Expression.Block(typeof(void), variables : new[] {secondVariable, firstVariable}, expressions : new Expression[] {parameter}),
                           Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[] {parameter}));

            TestNotEquivalent(Expression.Block(typeof(void), variables : new[] {firstVariable}, expressions : new Expression[] {parameter}),
                              Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[] {parameter}));

            TestNotEquivalent(Expression.Block(typeof(int), variables : new[] {secondVariable, firstVariable}, expressions : new Expression[] {parameter}),
                              Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[] {parameter}));

            TestNotEquivalent(Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[]
                {
                    Expression.Assign(firstVariable, Expression.Constant(2)),
                    Expression.AddAssign(parameter, firstVariable),
                }),
                              Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[]
                                  {
                                      parameter
                                  })
                );

            TestNotEquivalent(Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[]
                {
                    Expression.Assign(firstVariable, Expression.Constant(2)),
                    Expression.AddAssign(parameter, firstVariable),
                }),
                              Expression.Block(typeof(void), variables : new[] {firstVariable, secondVariable}, expressions : new Expression[]
                                  {
                                      Expression.Assign(firstVariable, Expression.Constant(3)),
                                      Expression.AddAssign(parameter, firstVariable),
                                  })
                );
        }

        [Test]
        public void TestNotSupportedExpressions()
        {
            TestThrows<NotImplementedException>(() => Expression.ArrayAccess(Expression.Parameter(typeof(int[,])), Expression.Constant(0), Expression.Constant(0)));
            TestThrows<NotImplementedException>(() => Expression.TypeIs(Expression.Parameter(typeof(object)), typeof(string)));
            TestThrows<NotImplementedException>(() => Expression.TypeEqual(Expression.Parameter(typeof(object)), typeof(string)));
            TestThrows<NotImplementedException>(() => Expression.TryCatch(Expression.Parameter(typeof(object)), Expression.Catch(typeof(Exception), Expression.Parameter(typeof(object)))));
            TestThrows<NotImplementedException>(() => Expression.Switch(Expression.Parameter(typeof(char)), Expression.SwitchCase(Expression.Block(typeof(void), Expression.Constant(42)), Expression.Constant('a'))));
            TestThrows<NotImplementedException>(() => Expression.RuntimeVariables(Expression.Parameter(typeof(object))));
            //TestThrows<NotImplementedException>(() => Expression.Dynamic());
            //TestThrows<NotImplementedException>(() => Expression.DebugInfo());
        }

        [Test]
        public void TestDefault()
        {
            TestEquivalent(Expression.Default(typeof(int)), Expression.Default(typeof(int)));
            TestNotEquivalent(Expression.Default(typeof(int)), Expression.Default(typeof(long)));
        }

        [Test]
        public void TestLabel()
        {
            TestEquivalent(Expression.Label(Expression.Label()), Expression.Label(Expression.Label()));
            TestEquivalent(Expression.Label(Expression.Label(typeof(void))), Expression.Label(Expression.Label(typeof(void))));
            TestEquivalent(Expression.Label(Expression.Label(typeof(void), "first")), Expression.Label(Expression.Label(typeof(void), "second")));

            TestNotEquivalent(Expression.Label(Expression.Label(typeof(int)), Expression.Constant(15)), Expression.Label(Expression.Label(typeof(long)), Expression.Constant(15L)));
            TestNotEquivalent(Expression.Label(Expression.Label(typeof(int)), Expression.Constant(15)), Expression.Label(Expression.Label(typeof(int)), Expression.Constant(16)));
        }

        [Test]
        public void TestGoto()
        {
            var gotoVoidCreators = new Func<LabelTarget, GotoExpression>[]
                {
                    Expression.Goto,
                    Expression.Return,
                    Expression.Break,
                    Expression.Continue
                };

            foreach (var gotoVoidCreator in gotoVoidCreators)
                TestEquivalent(gotoVoidCreator(Expression.Label(typeof(void))), gotoVoidCreator(Expression.Label(typeof(void))));

            foreach (var (first, second) in AllPairs(gotoVoidCreators))
                TestNotEquivalent(first(Expression.Label(typeof(void))), second(Expression.Label(typeof(void))));

            var gotoWithValueCreators = new Func<LabelTarget, Expression, GotoExpression>[]
                {
                    Expression.Goto,
                    Expression.Return,
                    Expression.Break,
                };

            var intParameter = Expression.Parameter(typeof(int));

            foreach (var gotoWithValueCreator in gotoWithValueCreators)
                TestEquivalent(gotoWithValueCreator(Expression.Label(typeof(int)), intParameter), gotoWithValueCreator(Expression.Label(typeof(int)), intParameter));

            foreach (var (first, second) in AllPairs(gotoWithValueCreators))
                TestNotEquivalent(first(Expression.Label(typeof(int)), intParameter), second(Expression.Label(typeof(int)), Expression.Parameter(typeof(int))));
        }

        [Test]
        public void TestLabelMapping()
        {
            var firstLabel = Expression.Label(typeof(void));
            var secondLabel = Expression.Label(typeof(void));

            TestEquivalent(Expression.Block(new Expression[]
                {
                    Expression.Goto(firstLabel),
                    Expression.Goto(secondLabel),
                    Expression.Goto(firstLabel),
                    Expression.Goto(secondLabel),
                }),
                           Expression.Block(new Expression[]
                               {
                                   Expression.Goto(firstLabel),
                                   Expression.Goto(secondLabel),
                                   Expression.Goto(firstLabel),
                                   Expression.Goto(secondLabel),
                               }));

            TestEquivalent(Expression.Block(new Expression[]
                {
                    Expression.Goto(firstLabel),
                    Expression.Goto(secondLabel),
                    Expression.Goto(firstLabel),
                    Expression.Goto(secondLabel),
                }),
                           Expression.Block(new Expression[]
                               {
                                   Expression.Goto(secondLabel),
                                   Expression.Goto(firstLabel),
                                   Expression.Goto(secondLabel),
                                   Expression.Goto(firstLabel),
                               }));

            TestNotEquivalent(Expression.Block(new Expression[]
                {
                    Expression.Goto(firstLabel),
                    Expression.Goto(secondLabel),
                    Expression.Goto(firstLabel),
                    Expression.Goto(secondLabel),
                }),
                              Expression.Block(new Expression[]
                                  {
                                      Expression.Goto(firstLabel),
                                      Expression.Goto(secondLabel),
                                      Expression.Goto(secondLabel),
                                      Expression.Goto(firstLabel),
                                  }));
        }

        [Test]
        public void TestParameterMapping()
        {
            var firstParameter = Expression.Parameter(typeof(int));
            var secondParameter = Expression.Parameter(typeof(int));

            TestEquivalent(Expression.Block(new Expression[]
                {
                    firstParameter,
                    secondParameter,
                    firstParameter,
                    secondParameter,
                }),
                           Expression.Block(new Expression[]
                               {
                                   firstParameter,
                                   secondParameter,
                                   firstParameter,
                                   secondParameter,
                               }), strictly : false);

            TestEquivalent(Expression.Block(new Expression[]
                {
                    firstParameter,
                    secondParameter,
                    firstParameter,
                    secondParameter,
                }),
                           Expression.Block(new Expression[]
                               {
                                   secondParameter,
                                   firstParameter,
                                   secondParameter,
                                   firstParameter,
                               }), strictly : false);

            TestNotEquivalent(Expression.Block(new Expression[]
                {
                    firstParameter,
                    secondParameter,
                    firstParameter,
                    secondParameter,
                }),
                              Expression.Block(new Expression[]
                                  {
                                      firstParameter,
                                      secondParameter,
                                      secondParameter,
                                      firstParameter,
                                  }), strictly : false);
        }

        [Test]
        public void TestLambda()
        {
            TestEquivalent((Expression<Func<int, int, int>>)((x, y) => x + y), (Expression<Func<int, int, int>>)((x, y) => x + y));

            TestNotEquivalent((Expression<Func<object, object>>)(x => x), (Expression<Func<object, object, object>>)((x, y) => x));

            TestNotEquivalent((Expression<Func<int, int, int>>)((x, y) => x + y), (Expression<Func<int, int, int>>)((x, y) => y + x));
        }

        [Test]
        public void TestInvoke()
        {
            Expression<Func<int, int, int>> lambda = (x, y) => x + y;

            var firstParameter = Expression.Constant(4);
            var secondParameter = Expression.Constant(8);

            TestEquivalent(Expression.Invoke(lambda, firstParameter, secondParameter), Expression.Invoke(lambda, firstParameter, secondParameter));

            TestNotEquivalent(Expression.Invoke(lambda, firstParameter, secondParameter), Expression.Invoke(lambda, firstParameter, firstParameter));
            TestNotEquivalent(Expression.Invoke(lambda, firstParameter, secondParameter), Expression.Invoke(lambda, secondParameter, secondParameter));
            TestNotEquivalent(Expression.Invoke(lambda, firstParameter, secondParameter), Expression.Invoke(lambda, secondParameter, firstParameter));

            Expression<Func<int, int, int>> otherLambda = (x, y) => y + x;
            TestNotEquivalent(Expression.Invoke(lambda, firstParameter, secondParameter), Expression.Invoke(otherLambda, firstParameter, secondParameter));
        }

        [Test]
        public void TestNew()
        {
            var firstConstructor = typeof(StringBuilder).GetConstructor(new[] {typeof(string)});
            var secondConstructor = typeof(FileInfo).GetConstructor(new[] {typeof(string)});
            Assert.That(firstConstructor, Is.Not.Null);
            Assert.That(secondConstructor, Is.Not.Null);

            var parameter = Expression.Parameter(typeof(string));
            TestEquivalent(Expression.New(firstConstructor, parameter), Expression.New(firstConstructor, parameter));
            TestNotEquivalent(Expression.New(firstConstructor, parameter), Expression.New(secondConstructor, parameter));
            TestNotEquivalent(Expression.New(firstConstructor, parameter), Expression.New(firstConstructor, Expression.Constant("zzz")));

            var constructor = typeof(string).GetConstructor(new[] {typeof(char), typeof(int)});
            Assert.That(constructor, Is.Not.Null);
            TestEquivalent(Expression.New(constructor, Expression.Constant('a'), Expression.Constant(2)), Expression.New(constructor, Expression.Constant('a'), Expression.Constant(2)));
            TestNotEquivalent(Expression.New(constructor, Expression.Constant('a'), Expression.Constant(2)), Expression.New(constructor, Expression.Constant('a'), Expression.Constant(3)));
            TestNotEquivalent(Expression.New(constructor, Expression.Constant('a'), Expression.Constant(2)), Expression.New(constructor, Expression.Constant('b'), Expression.Constant(2)));
        }

        [Test]
        public void TestNewArray()
        {
            TestEquivalent(Expression.NewArrayInit(typeof(int), Expression.Constant(4), Expression.Constant(8)),
                           Expression.NewArrayInit(typeof(int), Expression.Constant(4), Expression.Constant(8)));

            TestNotEquivalent(Expression.NewArrayInit(typeof(int), Expression.Constant(4), Expression.Constant(8)),
                              Expression.NewArrayInit(typeof(int), Expression.Constant(15), Expression.Constant(8)));

            TestNotEquivalent(Expression.NewArrayInit(typeof(int), Expression.Constant(4), Expression.Constant(8)),
                              Expression.NewArrayInit(typeof(int), Expression.Constant(4), Expression.Constant(16)));

            TestNotEquivalent(Expression.NewArrayInit(typeof(int), Expression.Constant(4), Expression.Constant(8)),
                              Expression.NewArrayInit(typeof(int), Expression.Constant(23), Expression.Constant(42)));
        }

        [Test]
        public void TestNewArrayBounds()
        {
            TestEquivalent(Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3)),
                           Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3)));

            TestNotEquivalent(Expression.NewArrayBounds(typeof(string), Expression.Constant(2), Expression.Constant(3)),
                              Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3)));

            TestNotEquivalent(Expression.NewArrayBounds(typeof(int), Expression.Constant(12), Expression.Constant(3)),
                              Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3)));

            TestNotEquivalent(Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(33)),
                              Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3)));

            TestNotEquivalent(Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3)),
                              Expression.NewArrayBounds(typeof(int), Expression.Constant(3), Expression.Constant(2)));
        }

        [Test]
        public void TestMemberAccess()
        {
            var lengthProperty = typeof(int[]).GetProperty("Length");
            Assert.That(lengthProperty, Is.Not.Null);

            var rankProperty = typeof(int[]).GetProperty("Rank");
            Assert.That(rankProperty, Is.Not.Null);

            var parameter = Expression.Parameter(typeof(int[]));
            TestEquivalent(Expression.MakeMemberAccess(parameter, lengthProperty),
                           Expression.MakeMemberAccess(parameter, lengthProperty));

            TestNotEquivalent(Expression.MakeMemberAccess(parameter, rankProperty),
                              Expression.MakeMemberAccess(parameter, lengthProperty));

            TestNotEquivalent(Expression.MakeMemberAccess(Expression.Constant(new[] {2, 3}), lengthProperty),
                              Expression.MakeMemberAccess(parameter, lengthProperty));
        }

        [Test]
        public void TestLoop()
        {
            var firstLabel = Expression.Label();
            var secondLabel = Expression.Label();

            var body = Expression.Parameter(typeof(int));

            TestEquivalent(Expression.Loop(body), Expression.Loop(body));
            TestNotEquivalent(Expression.Loop(Expression.Constant(2)), Expression.Loop(body));

            TestEquivalent(Expression.Loop(body, firstLabel), Expression.Loop(body, firstLabel));
            TestNotEquivalent(Expression.Loop(Expression.Constant(2), firstLabel), Expression.Loop(body, firstLabel));

            TestEquivalent(Expression.Loop(body, firstLabel, secondLabel), Expression.Loop(body, firstLabel, secondLabel));
            TestNotEquivalent(Expression.Loop(Expression.Constant(2), firstLabel, secondLabel), Expression.Loop(body, firstLabel, secondLabel));

            var complexBody = Expression.Block(Expression.Label(firstLabel), Expression.Label(secondLabel));
            TestEquivalent(Expression.Loop(complexBody, firstLabel, secondLabel),
                           Expression.Loop(complexBody, firstLabel, secondLabel));
            TestNotEquivalent(Expression.Loop(complexBody, firstLabel, secondLabel),
                              Expression.Loop(complexBody, secondLabel, firstLabel));
        }

        [Test]
        public void TestListInit()
        {
            var constructor = typeof(List<int>).GetConstructor(Type.EmptyTypes);
            Assert.That(constructor, Is.Not.Null);

            var listAddMethod = typeof(List<int>).GetMethod("Add");
            Assert.That(constructor, Is.Not.Null);

            TestEquivalent(Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(8)),
                           Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(8)));

            TestNotEquivalent(Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(8)),
                              Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(5), Expression.Constant(8)));

            TestNotEquivalent(Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(8)),
                              Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(10)));

            TestNotEquivalent(Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(8)),
                              Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(8), Expression.Constant(4)));

            var otherConstructor = typeof(List<int>).GetConstructor(new[] {typeof(int)});
            Assert.That(otherConstructor, Is.Not.Null);

            TestNotEquivalent(Expression.ListInit(Expression.New(constructor), listAddMethod, Expression.Constant(4), Expression.Constant(8)),
                              Expression.ListInit(Expression.New(otherConstructor, Expression.Parameter(typeof(int))), listAddMethod, Expression.Constant(4), Expression.Constant(8)));
        }

        [Test]
        public void TestMemberInitBind()
        {
            var constructor = typeof(ClassWithProperties).GetConstructor(Type.EmptyTypes);
            Assert.That(constructor, Is.Not.Null);

            var intPropertyInfo = typeof(ClassWithProperties).GetProperty("IntProperty");
            Assert.That(intPropertyInfo, Is.Not.Null);

            TestEquivalent(Expression.MemberInit(Expression.New(constructor), Expression.Bind(intPropertyInfo, Expression.Constant(5))),
                           Expression.MemberInit(Expression.New(constructor), Expression.Bind(intPropertyInfo, Expression.Constant(5)))
                );

            var otherConstructor = typeof(ClassWithProperties).GetConstructor(new[] {typeof(int)});
            Assert.That(otherConstructor, Is.Not.Null);

            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor), Expression.Bind(intPropertyInfo, Expression.Constant(5))),
                              Expression.MemberInit(Expression.New(otherConstructor, Expression.Parameter(typeof(int))), Expression.Bind(intPropertyInfo, Expression.Constant(5)))
                );

            var stringPropertyInfo = typeof(ClassWithProperties).GetProperty("StringProperty");
            Assert.That(stringPropertyInfo, Is.Not.Null);

            TestEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                 Expression.Bind(intPropertyInfo, Expression.Constant(5)),
                                                 Expression.Bind(stringPropertyInfo, Expression.Constant("zzz"))),
                           Expression.MemberInit(Expression.New(constructor),
                                                 Expression.Bind(intPropertyInfo, Expression.Constant(5)),
                                                 Expression.Bind(stringPropertyInfo, Expression.Constant("zzz")))
                );

            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.Bind(intPropertyInfo, Expression.Constant(5))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.Bind(intPropertyInfo, Expression.Constant(5)),
                                                    Expression.Bind(stringPropertyInfo, Expression.Constant("zzz")))
                );

            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.Bind(stringPropertyInfo, Expression.Constant("zzz")),
                                                    Expression.Bind(intPropertyInfo, Expression.Constant(5))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.Bind(intPropertyInfo, Expression.Constant(5)),
                                                    Expression.Bind(stringPropertyInfo, Expression.Constant("zzz")))
                );
        }

        [Test]
        public void TestMemberInitRecursive()
        {
            var constructor = typeof(ClassWithProperties).GetConstructor(Type.EmptyTypes);
            Assert.That(constructor, Is.Not.Null);

            var subclassProperty = typeof(ClassWithProperties).GetProperty("SubclassProperty");
            Assert.That(subclassProperty, Is.Not.Null);

            var subclassWithPropertyProperty = typeof(SubclassWithProperty).GetProperty("Property");
            Assert.That(subclassWithPropertyProperty, Is.Not.Null);

            TestEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                 Expression.MemberBind(subclassProperty, Expression.Bind(subclassWithPropertyProperty, Expression.Constant(5)))),
                           Expression.MemberInit(Expression.New(constructor),
                                                 Expression.MemberBind(subclassProperty, Expression.Bind(subclassWithPropertyProperty, Expression.Constant(5))))
                );

            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.MemberBind(subclassProperty, Expression.Bind(subclassWithPropertyProperty, Expression.Constant(5)))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.Bind(subclassProperty, Expression.Constant(new SubclassWithProperty())))
                );
        }

        [Test]
        public void TestMemberInitListInit()
        {
            var constructor = typeof(ClassWithProperties).GetConstructor(Type.EmptyTypes);
            Assert.That(constructor, Is.Not.Null);

            var intListProperty = typeof(ClassWithProperties).GetProperty("IntListProperty");
            Assert.That(intListProperty, Is.Not.Null);

            var listAddMethod = typeof(List<int>).GetMethod("Add");
            Assert.That(listAddMethod, Is.Not.Null);

            TestEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                 Expression.ListBind(intListProperty,
                                                                     Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                     Expression.ElementInit(listAddMethod, Expression.Constant(4)))),
                           Expression.MemberInit(Expression.New(constructor),
                                                 Expression.ListBind(intListProperty,
                                                                     Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                     Expression.ElementInit(listAddMethod, Expression.Constant(4))))
                );
            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4))))
                );
            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4)))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(5))))
                );
            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(92)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4)))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4))))
                );

            var otherIntListProperty = typeof(ClassWithProperties).GetProperty("OtherListProperty");
            Assert.That(otherIntListProperty, Is.Not.Null);

            TestNotEquivalent(Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(intListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4)))),
                              Expression.MemberInit(Expression.New(constructor),
                                                    Expression.ListBind(otherIntListProperty,
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(2)),
                                                                        Expression.ElementInit(listAddMethod, Expression.Constant(4))))
                );
        }

        private void TestThrows<TException>(Func<Expression> expressionCreationFunc) where TException : Exception
        {
            Assert.Throws<TException>(() => ExpressionEquivalenceChecker.Equivalent(expressionCreationFunc(), expressionCreationFunc(), strictly : false, distinguishEachAndCurrent : false));
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

            foreach (var (first, second) in AllPairs(binaryOperations))
            {
                TestNotEquivalent(first(firstParameter, secondParameter), second(firstParameter, secondParameter));
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

        private static IEnumerable<(TValue, TValue)> AllPairs<TValue>(IEnumerable<TValue> values)
        {
            var array = values.ToArray();
            for (var i = 0; i < array.Length; ++i)
            {
                for (var j = 0; j < i; ++j)
                    yield return (array[i], array[j]);
            }
        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        private class ObjectWithEqualsMethod
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public int Field { get; set; }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(obj, null))
                    return false;
                return obj is ObjectWithEqualsMethod other && Field == other.Field;
            }
        }

#pragma warning disable 660,661
        private class ObjectWithEqualityOperator
#pragma warning restore 660,661
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

        private class ClassWithProperties
        {
            public ClassWithProperties()
            {
            }

            public ClassWithProperties(int value)
            {
                IntProperty = value;
            }

            public int IntProperty { get; set; }

            public string StringProperty { get; set; }

            public SubclassWithProperty SubclassProperty { get; set; }

            public List<int> IntListProperty { get; set; }

            public List<int> OtherListProperty { get; set; }
        }

        private class SubclassWithProperty : ClassWithProperties
        {
            public int Property { get; set; }
        }

        private class ClassWithDictionary
        {
            public Dictionary<string, int> Dict { get; set; }
        }
    }
}