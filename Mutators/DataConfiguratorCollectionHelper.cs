using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using GrEmit;

namespace GrobExp.Mutators
{
    internal static class DataConfiguratorCollectionHelper
    {
        public static IMutatorsTreeCreator<TData> GetOrCreateMutatorsTreeCreator<TData>(Type[] path)
        {
            var types = path.Concat(new[] {typeof(TData)}).ToArray();
            var type = (Type)hashtable[path.Length];
            if (type == null)
            {
                lock (lockObject)
                {
                    type = (Type)hashtable[path.Length];
                    if (type == null)
                        hashtable[path.Length] = type = BuildMutatorsTreeCreator(types.Length);
                }
            }

            return (IMutatorsTreeCreator<TData>)Activator.CreateInstance(type.MakeGenericType(types));
        }

        public static MutatorsTreeBase<T> Merge<T>(MutatorsTreeBase<T> first, MutatorsTreeBase<T> second)
        {
            if (first == null) return second;
            if (second == null) return first;
            return first.Merge(second);
        }

        public const string AssemblyName = "MutatorsTreeCreators";

        public interface IMutatorsTreeCreator<TData>
        {
            MutatorsTreeBase<TData> GetMutatorsTree(IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory, IConverterCollectionFactory converterCollectionFactory, MutatorsContext[] mutatorsContexts, MutatorsContext[] converterContexts);
        }

        private static Type BuildMutatorsTreeCreator(int numberOfGenericParameters)
        {
            var typeBuilder = module.DefineType("MutatorsTreeCreator_" + numberOfGenericParameters, TypeAttributes.Public | TypeAttributes.Class);
            var genericParameters = typeBuilder.DefineGenericParameters(new[] {"TSource"}.Concat(new int[numberOfGenericParameters - 1].Select((i, index) => "T" + (index + 1))).ToArray());

            var interfaceType = typeof(IMutatorsTreeCreator<>).MakeGenericType(genericParameters.Last());
            var method = TypeBuilder.GetMethod(interfaceType, typeof(IMutatorsTreeCreator<>).GetMethod(getMutatorsTreeMethodName, BindingFlags.Public | BindingFlags.Instance));
            var methodBuilder = typeBuilder.DefineMethod(getMutatorsTreeMethodName, MethodAttributes.Public | MethodAttributes.Virtual, typeof(MutatorsTreeBase<>).MakeGenericType(genericParameters.Last()),
                                                         new[] {typeof(IDataConfiguratorCollectionFactory), typeof(IConverterCollectionFactory), typeof(MutatorsContext[]), typeof(MutatorsContext[])});
            using (var il = new GroboIL(methodBuilder))
            {
                il.Ldarg(1); // stack: [dataConfiguratorCollectionFactory]
                il.Call(getDataConfiguratorCollectionMethod.MakeGenericMethod(genericParameters[0]), typeof(IDataConfiguratorCollectionFactory)); // stack: [dataConfiguratorCollectionFactory.Get<TSource> = collection]

                var sourceCollectionIsNullLabel = il.DefineLabel("sourceCollectionIsNull");
                il.Dup(); // stack: [collection, collection]
                il.Brfalse(sourceCollectionIsNullLabel); // if(collection == null) goto sourceCollectionIsNull; stack: [collection]

                il.Ldarg(3); // stack: [collection, mutatorsContexts]
                il.Ldc_I4(0); // stack: [collection, mutatorsContexts, 0]
                il.Ldelem(typeof(MutatorsContext)); // stack: [collection, mutatorsContexts[0]]
                il.Ldc_I4(0); // stack: [collection, mutatorsContexts[0], 0]
                // todo ich: избавиться от константы
                var collectionType = typeof(IDataConfiguratorCollection<>).MakeGenericType(genericParameters[0]);
                il.Call(TypeBuilder.GetMethod(collectionType, typeof(IDataConfiguratorCollection<>).GetMethod("GetMutatorsTree", new[] {typeof(MutatorsContext), typeof(int)})), collectionType); // stack: [collection.GetMutatorsTree(mutatorsContexts[0], 0)]

                il.MarkLabel(sourceCollectionIsNullLabel);

                var current = il.DeclareLocal(typeof(MutatorsTreeBase<>).MakeGenericType(genericParameters[0]));
                for (var i = 0; i < numberOfGenericParameters - 1; ++i)
                {
                    // First: Migrate tree
                    il.Stloc(current);
                    il.Ldarg(2); // stack: [converterCollectionFactory]
                    il.Call(getConverterCollectionMethod.MakeGenericMethod(genericParameters[i + 1], genericParameters[i]), typeof(IConverterCollectionFactory)); // stack: [converterCollectionFactory.Get<T_i, T_i+1> = converterCollection]
                    il.Dup(); // stack: [converterCollection, converterCollection]
                    il.Ldloc(current); // stack: [converterCollection, converterCollection, current]
                    il.Ldarg(4); // stack: [converterCollection, converterCollection, current, converterContexts]
                    il.Ldc_I4(i); // stack: [converterCollection, converterCollection, current, converterContexts, i]
                    il.Ldelem(typeof(MutatorsContext)); // stack: [converterCollection, converterCollection, current, converterContexts[i]]
                    // todo ich: избавиться от константы
                    //il.Call(collectionType.GetMethod("Migrate", BindingFlags.Public | BindingFlags.Instance), collectionType); // stack: [converterCollection, converterCollection.Migrate(current, converterContexts[i])]
                    collectionType = typeof(IConverterCollection<,>).MakeGenericType(genericParameters[i + 1], genericParameters[i]);
                    il.Call(TypeBuilder.GetMethod(collectionType, typeof(IConverterCollection<,>).GetMethod("Migrate", BindingFlags.Public | BindingFlags.Instance)), collectionType); // stack: [converterCollection, converterCollection.Migrate(current, converterContexts[i])]
                    current = il.DeclareLocal(typeof(MutatorsTreeBase<>).MakeGenericType(genericParameters[i + 1]));
                    il.Stloc(current); // current = converterCollection.Migrate(current, converterContexts[i]); stack: [converterCollection]

                    // Second: Merge with validations tree
                    il.Ldarg(4); // stack: [converterCollection, converterContexts]
                    il.Ldc_I4(i); // stack: [converterCollection, converterContexts, i]
                    il.Ldelem(typeof(MutatorsContext)); // stack: [converterCollection, converterContexts[i]]
                    il.Ldc_I4(numberOfGenericParameters + i); // stack: [converterCollection, converterContexts[i], n + i]
                    // todo ich: избавиться от константы
                    //il.Call(collectionType.GetMethod("GetValidationsTree", BindingFlags.Public | BindingFlags.Instance), collectionType); // stack: [converterCollection.GetValidationsTree(converterContexts[i], n + i) = validationsTree]
                    il.Call(TypeBuilder.GetMethod(collectionType, typeof(IConverterCollection<,>).GetMethod("GetValidationsTree", BindingFlags.Public | BindingFlags.Instance)), collectionType); // stack: [converterCollection.GetValidationsTree(converterContexts[i], n + i) = validationsTree]
                    il.Ldloc(current); // stack: [validationsTree, current]
                    //var mutatorsTreeType = typeof(MutatorsTreeBase<>).MakeGenericType(genericParameters[i + 1]);
                    //il.Call(mutatorsTreeType.GetMethod("Merge", BindingFlags.Public | BindingFlags.Instance), mutatorsTreeType); // stack: [validationsTree.Merge(current)]
                    //il.Call(TypeBuilder.GetMethod(mutatorsTreeType, typeof(MutatorsTreeBase<>).GetMethod("Merge", BindingFlags.Public | BindingFlags.Instance)), mutatorsTreeType); // stack: [validationsTree.Merge(current)]
                    il.Call(mergeMethod.MakeGenericMethod(genericParameters[i + 1])); // stack: [validationsTree.Merge(current)]

                    // Third: Merge with current mutators tree
                    il.Ldarg(1); // stack: [validationsTree.Merge(current), dataConfiguratorCollectionFactory]
                    il.Call(getDataConfiguratorCollectionMethod.MakeGenericMethod(genericParameters[i + 1]), typeof(IDataConfiguratorCollectionFactory)); // stack: [validationsTree.Merge(current), dataConfiguratorCollectionFactory.Get<T_i+1> = collection]

                    var collectionIsNullLabel = il.DefineLabel("collectionIsNull");
                    il.Dup(); // stack: [validationsTree.Merge(current), collection, collection]
                    il.Brfalse(collectionIsNullLabel); // if(collection == null) goto collectionIsNull; stack: [validationsTree.Merge(current), collection]

                    il.Ldarg(3); // stack: [validationsTree.Merge(current), collection, mutatorsContexts]
                    il.Ldc_I4(i + 1); // stack: [validationsTree.Merge(current), collection, mutatorsContexts, i + 1]
                    il.Ldelem(typeof(MutatorsContext)); // stack: [validationsTree.Merge(current), collection, mutatorsContexts[i + 1]]
                    il.Ldc_I4(i + 1); // stack: [validationsTree.Merge(current), collection, mutatorsContexts[i + 1], 0]
                    // todo ich: избавиться от константы
                    collectionType = typeof(IDataConfiguratorCollection<>).MakeGenericType(genericParameters[i + 1]);
                    il.Call(TypeBuilder.GetMethod(collectionType, typeof(IDataConfiguratorCollection<>).GetMethod("GetMutatorsTree", new[] {typeof(MutatorsContext), typeof(int)})), collectionType); // stack: [validationsTree.Merge(current), collection.GetMutatorsTree(mutatorsContexts[i + 1], 0)]

                    il.MarkLabel(collectionIsNullLabel);

                    //il.Call(TypeBuilder.GetMethod(mutatorsTreeType, typeof(MutatorsTreeBase<>).GetMethod("Merge", BindingFlags.Public | BindingFlags.Instance)), mutatorsTreeType); // stack: [validationsTree.Merge(current).Merge(collection.GetMutatorsTree(mutatorsContexts[i + 1], 0)) = current]
                    il.Call(mergeMethod.MakeGenericMethod(genericParameters[i + 1])); // stack: [validationsTree.Merge(current).Merge(collection.GetMutatorsTree(mutatorsContexts[i + 1], 0)) = current]
                }

                il.Ret();
            }

            typeBuilder.DefineMethodOverride(methodBuilder, method);
            typeBuilder.AddInterfaceImplementation(interfaceType);
            return typeBuilder.CreateType();
        }

        private static readonly string getMutatorsTreeMethodName = ((MethodCallExpression)((Expression<Func<IMutatorsTreeCreator<int>, MutatorsTreeBase<int>>>)(creator => creator.GetMutatorsTree(null, null, null, null))).Body).Method.Name;
        private static readonly MethodInfo getDataConfiguratorCollectionMethod = ((MethodCallExpression)((Expression<Func<IDataConfiguratorCollectionFactory, IDataConfiguratorCollection<int>>>)(factory => factory.Get<int>())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo getConverterCollectionMethod = ((MethodCallExpression)((Expression<Func<IConverterCollectionFactory, IConverterCollection<int, int>>>)(factory => factory.Get<int, int>())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo mergeMethod = ((MethodCallExpression)((Expression<Func<MutatorsTreeBase<int>, MutatorsTreeBase<int>, MutatorsTreeBase<int>>>)((first, second) => Merge(first, second))).Body).Method.GetGenericMethodDefinition();

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        private static readonly object lockObject = new object();

        private static readonly Hashtable hashtable = new Hashtable();
    }
}