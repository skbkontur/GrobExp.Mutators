using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GrobExp.Mutators.Visitors
{
    public class ArrayIndexResolver
    {
        // todo ich: слишком медленный монстр, убить
        public Expression Resolve(Expression exp)
        {

            ParameterExpression[] indexes;
            var withoutLinq = new LinqEliminator().Eliminate(exp, out indexes);
            var paths = ExpressionPathsBuilder.BuildPaths(exp, indexes);
            return Expression.Block(indexes, withoutLinq, paths);

//            //return Expression.Constant(new[] {"root"}, typeof(string[]));
//            var resolveInternal = ResolveInternal(exp, false);
//            var extendNulls = resolveInternal;
//            ParameterExpression indexes = Expression.Variable(typeof(int[]));
//            Expression indexesInit = Expression.Assign(indexes, Expression.MakeMemberAccess(extendNulls, extendNulls.Type.GetProperty("Indexes", BindingFlags.Public | BindingFlags.Instance)));
//            var spine = Expression.Lambda(exp, exp.ExtractParameters()).ExtractPrimaryDependencies().Single().Body;
//            var result = new List<Expression>();
//            var shards = spine.SmashToSmithereens();
//            int k = 0;
//            for(int i = 1; i < shards.Length; ++i)
//            {
//                var shard = shards[i];
//                switch(shard.NodeType)
//                {
//                case ExpressionType.Convert:
//                    break;
//                case ExpressionType.MemberAccess:
//                    {
//                        var memberExpression = (MemberExpression)shard;
//                        result.Add(Expression.Constant(memberExpression.Member.Name));
//                        break;
//                    }
//                case ExpressionType.ArrayIndex:
//                    {
//                        var binaryExpression = (BinaryExpression)shard;
//                        result.Add(Expression.Call(binaryExpression.Right, "ToString", Type.EmptyTypes));
//                        break;
//                    }
//                case ExpressionType.Call:
//                    {
//                        var methodCallExpression = (MethodCallExpression)shard;
//                        /*if(methodCallExpression.Method.IsCurrentMethod())
//                        {
////                            Expression array = methodCallExpression.Arguments.Single();
////                            Type elementType = array.Type.GetElementType();
////                            Expression index = Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(elementType), Expression.Call(MutatorsHelperFunctions.CurrentMethod.MakeGenericMethod(elementType), array));
//                            result.Add(Expression.Call(globalIndexes[l++], "ToString", Type.EmptyTypes));
//                        }
//                        else*/
//                        if(methodCallExpression.Method.IsEachMethod())
//                        {
//                            result.Add(Expression.Condition(Expression.LessThan(Expression.Constant(k), Expression.ArrayLength(indexes)), Expression.Call(Expression.ArrayIndex(indexes, Expression.Constant(k)), "ToString", Type.EmptyTypes), Expression.Constant("-1")));
//                            ++k;
//                        }
//                        else if(methodCallExpression.Method.IsIndexerGetter())
//                            result.AddRange(methodCallExpression.Arguments);
//                        else
//                            throw new NotSupportedException("Method " + methodCallExpression.Method + " is not supported");
//                        break;
//                    }
//                default:
//                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
//                }
//            }
//
////            Expression writeExp = Expression.Call(consoleWriteLineMethod, new[] {Expression.Constant(exp.ToString())});
////            Expression writeIndexes = Expression.Call(consoleWriteLineMethod, new[] {Expression.Call(stringJoinMethod, new Expression[] {Expression.Constant(", "), indexes})});
//
//            return Expression.Block(typeof(string[]), new[] {indexes}, indexesInit, /*writeExp, writeIndexes,*/ Expression.NewArrayInit(typeof(string), result));
        }

        //private static readonly MethodInfo consoleWriteLineMethod = ((MethodCallExpression)((Expression<Action<string>>)(s => Console.WriteLine(s))).Body).Method;
        //private static readonly MethodInfo stringJoinMethod = ((MethodCallExpression)((Expression<Func<int[], string>>)(ints => string.Join(".", ints))).Body).Method;

        private static bool IsArrayIndexing(Expression exp)
        {
            return exp.NodeType == ExpressionType.ArrayIndex || (exp.NodeType == ExpressionType.Call && ((MethodCallExpression)exp).Method.IsEachMethod());
        }

        private static Type CreateAnonymousType(Type[] types, string[] names)
        {
            var array = new TypesWithNamesArray(types, names);
            var type = (Type)anonymousTypes[array];
            if(type == null)
            {
                lock(anonymousTypesLock)
                {
                    type = (Type)anonymousTypes[array];
                    if(type == null)
                    {
                        type = BuildType(types, names);
                        anonymousTypes[array] = type;
                    }
                }
            }
            return type;
        }

        private static bool IsDynamicAnonymousType(Type type)
        {
            return type.Name.StartsWith("<>f__DynamicAnonymousType_");
        }

        private static Type BuildType(Type[] types, string[] names)
        {
            int length = types.Length;
            if(names.Length != length)
                throw new InvalidOperationException();
            var typeBuilder = module.DefineType("<>f__DynamicAnonymousType_" + Guid.NewGuid(), TypeAttributes.Public | TypeAttributes.Class);
            var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, types);
            var constructorIl = constructor.GetILGenerator();
            for(int i = 0; i < length; ++i)
            {
                var field = typeBuilder.DefineField(names[i] + "_" + Guid.NewGuid(), types[i], FieldAttributes.Private | FieldAttributes.InitOnly);
                var property = typeBuilder.DefineProperty(names[i], PropertyAttributes.None, types[i], Type.EmptyTypes);
                var setter = typeBuilder.DefineMethod(names[i] + "_setter" + Guid.NewGuid(), MethodAttributes.Public, CallingConventions.HasThis, typeof(void), new[] {types[i]});
                var il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // stack: [this]
                il.Emit(OpCodes.Ldarg_1); // stack: [this, value]
                il.Emit(OpCodes.Stfld, field); // this.field = value
                il.Emit(OpCodes.Ret);
                property.SetSetMethod(setter);

                var getter = typeBuilder.DefineMethod(names[i] + "_getter" + Guid.NewGuid(), MethodAttributes.Public, CallingConventions.HasThis, types[i], Type.EmptyTypes);
                il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // stack: [this]
                il.Emit(OpCodes.Ldfld, field); // stack: [this.field]
                il.Emit(OpCodes.Ret);
                property.SetGetMethod(getter);

                constructorIl.Emit(OpCodes.Ldarg_0); // stack: [this]
                constructorIl.Emit(OpCodes.Ldarg_S, i + 1); // stack: [this, arg_i]
                constructorIl.Emit(OpCodes.Call, setter); // this.setter(arg_i)
            }
            constructorIl.Emit(OpCodes.Ret);
            return typeBuilder.CreateType();
        }

        private Expression ResolveInternal(Expression exp, bool wrapCollections)
        {
            if(!exp.IsLinkOfChain(true, true))
            {
                if(!exp.IsAnonymousTypeCreation())
                    return exp;
                //throw new NotSupportedException("A chain or an anonymous type creation expected but was " + ExpressionCompiler.DebugViewGetter(exp));
                var resolvedArguments = ((NewExpression)exp).Arguments.Select(arg => ResolveInternal(arg, wrapCollections)).ToArray();
                var types = resolvedArguments.Select(arg => arg.Type).ToArray();
                var names = exp.Type.GetProperties().Select(property => property.Name).ToArray();
                var type = CreateAnonymousType(types, names);
                var constructor = type.GetConstructor(types);
                if(constructor == null)
                    throw new Exception("Missing constructor of type '" + Formatter.Format(type) + "' with parameters {" + string.Join(", ", types.Select(Formatter.Format)) + "}");
                return Expression.New(constructor, resolvedArguments);
            }
            Expression[] shards = exp.SmashToSmithereens();
            Expression current = shards[0];
            var indexes = Expression.Variable(typeof(List<int>));
            var variables = new List<ParameterExpression> {indexes};
            var expressions = new List<Expression> {Expression.Assign(indexes, Expression.New(typeof(List<int>)))};

            for(int i = 1; i < shards.Length; i++)
            {
                var shard = shards[i];
                switch(shard.NodeType)
                {
                case ExpressionType.Convert:
                    break;
                case ExpressionType.MemberAccess:
                    var member = ((MemberExpression)shard).Member;
                    if(!(current.Type.IsGenericType && current.Type.GetGenericTypeDefinition() == typeof(IndexedValue<>) && member.Name == "Value"))
                        current = Expression.MakeMemberAccess(current, member);
                    else
                    {
                        ParameterExpression temp = Expression.Variable(current.Type);
                        variables.Add(temp);
                        expressions.Add(Expression.Assign(temp, current /*.ExtendNulls()*/));
                        expressions.Add(Expression.IfThen(Expression.NotEqual(temp, Expression.Constant(null, temp.Type)), Expression.Call(indexes, listIntsAddRangeMethod, new[] {Expression.MakeMemberAccess(temp, current.Type.GetProperty("Indexes", BindingFlags.Public | BindingFlags.Instance))})));
                        current = Expression.MakeMemberAccess(temp, member);
                    }
                    if(current.Type.IsArray && (i == shards.Length - 1 || !IsArrayIndexing(shards[i + 1])))
                    {
                        Type elementType = current.Type.GetElementType();
                        ParameterExpression parameter = Expression.Parameter(elementType);
                        ParameterExpression index = Expression.Parameter(typeof(int));
                        var types = new[] {elementType, typeof(IEnumerable<int>)};
                        var type = typeof(IndexedValue<>).MakeGenericType(elementType);
                        ConstructorInfo constructor = type.GetConstructor(types);
                        if(constructor == null)
                            throw new Exception("Missing constructor of type '" + Formatter.Format(type) + "' with parameters {" + string.Join(", ", types.Select(Formatter.Format)) + "}");
                        Expression newExpression = Expression.New(constructor, parameter, Expression.NewArrayInit(typeof(int), index));
                        LambdaExpression selector = Expression.Lambda(newExpression, parameter, index);
                        //expressions.Add(Expression.Call(consoleWriteLineMethod, new[] {Expression.Call(Expression.ArrayLength(current), "ToString", Type.EmptyTypes)}));
                        current = Expression.Call(selectWithIndexMethod.MakeGenericMethod(elementType, type), new[] {current, selector});
                    }
                    break;
                case ExpressionType.ArrayIndex:
                    {
                        var itemType = current.Type.GetItemType();
                        if(!current.Type.IsArray)
                            current = Expression.Call(toArrayMethod.MakeGenericMethod(itemType), current);
                        current = Expression.ArrayIndex(current, ((BinaryExpression)shard).Right);
                        if(itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(IndexedValue<>))
                            current = Expression.MakeMemberAccess(current, itemType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance));
                    }
                    break;
                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)shard;
                    var method = methodCallExpression.Method;
                    var arguments = (method.IsExtension() ? methodCallExpression.Arguments.Skip(1) : methodCallExpression.Arguments).ToArray();
                    if(method.IsGenericMethod)
                        method = method.GetGenericMethodDefinition();

//                    if (method == MutatorsHelperFunctions.CurrentMethod)
//                    {
////                        Expression array = arguments.Single();
////                        Type elementType = array.Type.GetElementType();
////                        Expression index = Expression.Call(MutatorsHelperFunctions.CurrentIndexMethod.MakeGenericMethod(elementType), Expression.Call(MutatorsHelperFunctions.EachMethod.MakeGenericMethod(elementType), array));
////                        result.Add(Expression.Call(index, "ToString", Type.EmptyTypes));
//                        current = Expression.Call(method.MakeGenericMethod(GetElementType(current.Type)), new[] {current});
//                        break;
//                    }
                    if(method.DeclaringType == typeof(Enumerable))
                    {
                        switch(method.Name)
                        {
                        case "SingleOrDefault":
                        case "FirstOrDefault":
                            {
                                Type argumentType = current.Type.GetItemType();
                                var predicate = (LambdaExpression)arguments.SingleOrDefault();
                                current = predicate == null
                                              ? Expression.Call(method.MakeGenericMethod(argumentType), new[] {current})
                                              : Expression.Call(method.MakeGenericMethod(argumentType), new[] {current, Merge(predicate, argumentType)});
                                ParameterExpression temp = Expression.Parameter(current.Type);
                                variables.Add(temp);
                                expressions.Add(Expression.Assign(temp, current /*.ExtendNulls()*/));
                                expressions.Add(Expression.IfThen(Expression.NotEqual(temp, Expression.Constant(null, temp.Type)), Expression.Call(indexes, listIntsAddRangeMethod, new[] {Expression.MakeMemberAccess(temp, current.Type.GetProperty("Indexes", BindingFlags.Public | BindingFlags.Instance))})));
                                current = Expression.MakeMemberAccess(temp, argumentType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance));
                                break;
                            }
                        case "Where":
                            {
                                Type argumentType = current.Type.GetItemType();
                                var predicate = (LambdaExpression)arguments.Single();
                                current = Expression.Call(method.MakeGenericMethod(argumentType), new[] {current, Merge(predicate, argumentType)});
                                break;
                            }
                        case "Select":
                            {
                                Type argumentType = current.Type.GetItemType();
                                var selector = Merge((LambdaExpression)arguments.Single(), argumentType);
                                var resolvedSelector = ResolveInternal(selector.Body, false);
                                current = Expression.Call(method.MakeGenericMethod(new[] {argumentType, resolvedSelector.Type}), new[] {current, Expression.Lambda(resolvedSelector, selector.Parameters)});
                                break;
                            }
                        case "SelectMany":
                            {
                                Type argumentType = current.Type.GetItemType();
                                var collectionSelector = Merge((LambdaExpression)arguments.First(), argumentType);
                                var resolvedCollectionSelector = ResolveInternal(collectionSelector.Body, true);
                                Type itemType = resolvedCollectionSelector.Type.GetItemType();
                                var resultSelector = Merge((LambdaExpression)arguments.Skip(1).SingleOrDefault(), argumentType, itemType);
                                if(resultSelector == null)
                                    current = Expression.Call(method.MakeGenericMethod(argumentType, itemType), new[] {current, Expression.Lambda(resolvedCollectionSelector, collectionSelector.Parameters)});
                                else
                                {
                                    var resolvedResultSelector = ResolveInternal(resultSelector.Body, false);
                                    current = Expression.Call(method.MakeGenericMethod(argumentType, itemType, resolvedResultSelector.Type), new[] {current, Expression.Lambda(resolvedCollectionSelector, collectionSelector.Parameters), Expression.Lambda(resolvedResultSelector, resultSelector.Parameters)});
                                }
                                break;
                            }
                        case "ToArray":
                            {
                                current = Expression.Call(method.MakeGenericMethod(current.Type.GetItemType()), current);
                                break;
                            }
                        default:
                            throw new NotSupportedException("Method '" + methodCallExpression.Method + "' is not supported");
                        }
                    }
                    else if(method.IsIndexerGetter())
                        current = Expression.Call(current, method, methodCallExpression.Arguments);
                    else
                        throw new NotSupportedException("Method '" + methodCallExpression.Method + "' is not supported");
                    break;
                default:
                    throw new NotSupportedException("Node type '" + shard.NodeType + "' is not supported");
                }
            }

            if(current.Type.IsGenericType && current.Type.GetGenericTypeDefinition() == typeof(IndexedValue<>))
            {
                var type = current.Type;
                var types = type.GetGenericArguments();
                ConstructorInfo constructor = type.GetConstructor(types);
                if(constructor == null)
                    throw new Exception("Missing constructor of type '" + Formatter.Format(type) + "' with parameters {" + string.Join(", ", types.Select(Formatter.Format)) + "}");
                ParameterExpression temp = Expression.Variable(current.Type);
                Expression assign = Expression.Assign(temp, current /*.ExtendNulls()*/);
                Expression firstArgument = Expression.MakeMemberAccess(temp, type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)) /*.ExtendNulls()*/;
                Expression nextIndexes = Expression.MakeMemberAccess(temp, type.GetProperty("Indexes", BindingFlags.Public | BindingFlags.Instance)) /*.ExtendNulls()*/;
                Expression secondArgument = Expression.Call(concatIntsMethod, new[] {indexes, nextIndexes});
                var result = Expression.New(constructor, firstArgument, secondArgument);
                return Expression.Block(type, variables.Concat(new[] {temp}), expressions.Concat(new[] {assign, result}));
            }
            if(wrapCollections && current.Type.IsGenericType && current.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type itemType = current.Type.GetItemType();
                if(!(itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(IndexedValue<>)))
                    throw new InvalidOperationException();
                ParameterExpression parameter = Expression.Parameter(itemType);
                var types = new[] {itemType.GetGenericArguments().Single(), typeof(IEnumerable<int>)};
                ConstructorInfo constructor = itemType.GetConstructor(types);
                if(constructor == null)
                    throw new Exception("Missing constructor of type '" + Formatter.Format(itemType) + "' with parameters {" + string.Join(", ", types.Select(Formatter.Format)) + "}");
                Expression value = Expression.MakeMemberAccess(parameter, itemType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance));
                Expression nextIndexes = Expression.MakeMemberAccess(parameter, itemType.GetProperty("Indexes", BindingFlags.Public | BindingFlags.Instance));
                Expression newExpression = Expression.New(constructor, value, Expression.Call(concatIntsMethod, new[] {indexes, nextIndexes}));
                LambdaExpression selector = Expression.Lambda(newExpression, parameter);
                current = Expression.Call(selectMethod.MakeGenericMethod(itemType, itemType), new[] {current, selector});
                return Expression.Block(current.Type, variables, expressions.Concat(new[] {current /*.ExtendNulls()*/}));
            }
            else
            {
                var types = new[] {current.Type, typeof(int[])};
                var type = typeof(IndexedValue<>).MakeGenericType(current.Type);
                ConstructorInfo constructor = type.GetConstructor(types);
                if(constructor == null)
                    throw new Exception("Missing constructor of type '" + Formatter.Format(type) + "' with parameters {" + string.Join(", ", types.Select(Formatter.Format)) + "}");
                var result = Expression.New(constructor, current /*.ExtendNulls()*/, indexes);
                return Expression.Block(type, variables, expressions.Concat(new[] {result}));
            }
        }

        private static LambdaExpression Merge(LambdaExpression lambda, Type type)
        {
            if(lambda == null) return null;
            if(!IsDynamicAnonymousType(type))
            {
                var parameter = Expression.Parameter(type);
                return Expression.Lambda(Expression.MakeMemberAccess(parameter, type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)), parameter).Merge(lambda);
            }
            else
            {
                var parameter = Expression.Parameter(type);
                var lambdaParameter = lambda.Parameters.Single();
                if(!lambdaParameter.Type.IsAnonymousType())
                    throw new InvalidOperationException("An anonymous type expected but was " + lambdaParameter.Type);
                return Expression.Lambda(new AnonymousTypeReplacer(lambdaParameter, parameter).Visit(lambda.Body), parameter);
            }
        }

        private static LambdaExpression Merge(LambdaExpression lambda, Type type1, Type type2)
        {
            if(lambda == null) return null;
            var parameter1 = Expression.Parameter(type1);
            var parameter2 = Expression.Parameter(type2);
            var merger = new ExpressionMerger(Expression.Lambda(Expression.MakeMemberAccess(parameter1, type1.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)), parameter1), Expression.Lambda(Expression.MakeMemberAccess(parameter2, type2.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)), parameter2));
            return merger.Merge(lambda);
        }

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
        private static readonly Hashtable anonymousTypes = new Hashtable();
        private static readonly object anonymousTypesLock = new object();

        private static readonly MethodInfo selectMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, IEnumerable<int>>>)(arr => arr.Select(i => i))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo selectWithIndexMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, IEnumerable<int>>>)(arr => arr.Select((i, index) => index))).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo concatIntsMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, IEnumerable<int>, IEnumerable<int>>>)((ints1, ints2) => ints1.Concat(ints2))).Body).Method;
        private static readonly MethodInfo listIntsAddRangeMethod = ((MethodCallExpression)((Expression<Action<List<int>>>)(list => list.AddRange(new int[0]))).Body).Method;
        private static readonly MethodInfo toArrayMethod = ((MethodCallExpression)((Expression<Func<IEnumerable<int>, int[]>>)(enumerable => enumerable.ToArray())).Body).Method.GetGenericMethodDefinition();

        private class TypesWithNamesArray
        {
            public TypesWithNamesArray(Type[] types, string[] names)
            {
                Types = types;
                Names = names;
            }

            public override int GetHashCode()
            {
                return Names.Aggregate(Types.Aggregate(0, (current, t) => current * 314159265 + t.GetHashCode()), (current, t) => current * 271828459 + t.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                if(!(obj is TypesWithNamesArray))
                    return false;
                if(ReferenceEquals(this, obj)) return true;
                var other = (TypesWithNamesArray)obj;
                if(Types.Length != other.Types.Length || Names.Length != other.Names.Length)
                    return false;
                if(Types.Where((t, i) => t != other.Types[i]).Any())
                    return false;
                if(Names.Where((s, i) => s != other.Names[i]).Any())
                    return false;
                return true;
            }

            public Type[] Types { get; private set; }
            public string[] Names { get; private set; }
        }

        private class IndexedValue<T>
        {
            public IndexedValue(T value, IEnumerable<int> indexes)
            {
                Value = value;
                Indexes = indexes == null ? new int[0] : indexes.ToArray();
            }

            public T Value { get; private set; }
            public int[] Indexes { get; private set; }
        }

        private class AnonymousTypeReplacer : ExpressionVisitor
        {
            public AnonymousTypeReplacer(ParameterExpression from, ParameterExpression to)
            {
                this.to = to;
                this.from = from;
                properties = to.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(property => property.Name);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if(node.Expression == from)
                {
                    PropertyInfo property = properties[node.Member.Name];
                    return Expression.MakeMemberAccess(Expression.MakeMemberAccess(to, property), property.PropertyType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance));
                }
                return base.VisitMember(node);
            }

            private readonly ParameterExpression to;
            private readonly ParameterExpression from;
            private readonly Dictionary<string, PropertyInfo> properties;
        }
    }
}