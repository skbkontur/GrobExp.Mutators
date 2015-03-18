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
            if(type == null)
            {
                lock(lockObject)
                {
                    type = (Type)hashtable[path.Length];
                    if(type == null)
                        hashtable[path.Length] = type = BuildMutatorsTreeCreator(types.Length);
                }
            }
            return (IMutatorsTreeCreator<TData>)Activator.CreateInstance(type.MakeGenericType(types));
        }

        public static MutatorsTree<T> Merge<T>(MutatorsTree<T> first, MutatorsTree<T> second)
        {
            if(first == null) return second;
            if(second == null) return first;
            return first.Merge(second);
        }

        public const string AssemblyName = "MutatorsTreeCreators";

        public interface IMutatorsTreeCreator<TData>
        {
            MutatorsTree<TData> GetMutatorsTree(IDataConfiguratorCollectionFactory dataConfiguratorCollectionFactory, IConverterCollectionFactory converterCollectionFactory, MutatorsContext[] mutatorsContexts, MutatorsContext[] converterContexts);
        }

        private static Type BuildMutatorsTreeCreator(int numberOfGenericParameters)
        {
            var typeBuilder = module.DefineType("MutatorsTreeCreator_" + numberOfGenericParameters, TypeAttributes.Public | TypeAttributes.Class);
            var genericParameters = typeBuilder.DefineGenericParameters(new[] {"TSource"}.Concat(new int[numberOfGenericParameters - 1].Select((i, index) => "T" + (index + 1))).ToArray());

            var interfaceType = typeof(IMutatorsTreeCreator<>).MakeGenericType(genericParameters.Last());
            var method = TypeBuilder.GetMethod(interfaceType, typeof(IMutatorsTreeCreator<>).GetMethod(getMutatorsTreeMethodName, BindingFlags.Public | BindingFlags.Instance));
            var methodBuilder = typeBuilder.DefineMethod(getMutatorsTreeMethodName, MethodAttributes.Public | MethodAttributes.Virtual, typeof(MutatorsTree<>).MakeGenericType(genericParameters.Last()),
                                                         new[] {typeof(IDataConfiguratorCollectionFactory), typeof(IConverterCollectionFactory), typeof(MutatorsContext[]), typeof(MutatorsContext[])});
            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1); // stack: [dataConfiguratorCollectionFactory]
            il.EmitCall(OpCodes.Callvirt, getDataConfiguratorCollectionMethod.MakeGenericMethod(genericParameters[0]), null); // stack: [dataConfiguratorCollectionFactory.Get<TSource> = collection]

            var sourceCollectionIsNullLabel = il.DefineLabel();
            il.Emit(OpCodes.Dup); // stack: [collection, collection]
            il.Emit(OpCodes.Brfalse, sourceCollectionIsNullLabel); // if(collection == null) goto sourceCollectionIsNull; stack: [collection]

            il.Emit(OpCodes.Ldarg_3); // stack: [collection, mutatorsContexts]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [collection, mutatorsContexts, 0]
            il.Emit(OpCodes.Ldelem, typeof(MutatorsContext)); // stack: [collection, mutatorsContexts[0]]
            il.Emit(OpCodes.Ldc_I4_0); // stack: [collection, mutatorsContexts[0], 0]
            // todo ich: избавиться от константы
            var collectionType = typeof(IDataConfiguratorCollection<>).MakeGenericType(genericParameters[0]);
            il.EmitCall(OpCodes.Callvirt, TypeBuilder.GetMethod(collectionType, typeof(IDataConfiguratorCollection<>).GetMethod("GetMutatorsTree", new[] {typeof(MutatorsContext), typeof(int)})), null); // stack: [collection.GetMutatorsTree(mutatorsContexts[0], 0)]

            il.MarkLabel(sourceCollectionIsNullLabel);

            var current = il.DeclareLocal(typeof(object));

            for(var i = 0; i < numberOfGenericParameters - 1; ++i)
            {
                // First: Migrate tree
                il.Emit(OpCodes.Stloc, current);
                il.Emit(OpCodes.Ldarg_2); // stack: [converterCollectionFactory]
                il.EmitCall(OpCodes.Callvirt, getConverterCollectionMethod.MakeGenericMethod(genericParameters[i + 1], genericParameters[i]), null); // stack: [converterCollectionFactory.Get<T_i, T_i+1> = converterCollection]
                il.Emit(OpCodes.Dup); // stack: [converterCollection, converterCollection]
                il.Emit(OpCodes.Ldloc, current); // stack: [converterCollection, converterCollection, current]
                il.Emit(OpCodes.Ldarg, 4); // stack: [converterCollection, converterCollection, current, converterContexts]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [converterCollection, converterCollection, current, converterContexts, i]
                il.Emit(OpCodes.Ldelem, typeof(MutatorsContext)); // stack: [converterCollection, converterCollection, current, converterContexts[i]]
                // todo ich: избавиться от константы
                //il.Call(collectionType.GetMethod("Migrate", BindingFlags.Public | BindingFlags.Instance), collectionType); // stack: [converterCollection, converterCollection.Migrate(current, converterContexts[i])]
                collectionType = typeof(IConverterCollection<,>).MakeGenericType(genericParameters[i + 1], genericParameters[i]);
                il.EmitCall(OpCodes.Callvirt, TypeBuilder.GetMethod(collectionType, typeof(IConverterCollection<,>).GetMethod("Migrate", BindingFlags.Public | BindingFlags.Instance)), null); // stack: [converterCollection, converterCollection.Migrate(current, converterContexts[i])]
                il.Emit(OpCodes.Stloc, current); // current = converterCollection.Migrate(current, converterContexts[i]); stack: [converterCollection]

                // Second: Merge with validations tree
                il.Emit(OpCodes.Ldarg, 4); // stack: [converterCollection, converterContexts]
                il.Emit(OpCodes.Ldc_I4, i); // stack: [converterCollection, converterContexts, i]
                il.Emit(OpCodes.Ldelem, typeof(MutatorsContext)); // stack: [converterCollection, converterContexts[i]]
                il.Emit(OpCodes.Ldc_I4, numberOfGenericParameters + i); // stack: [converterCollection, converterContexts[i], n + i]
                // todo ich: избавиться от константы
                //il.Call(collectionType.GetMethod("GetValidationsTree", BindingFlags.Public | BindingFlags.Instance), collectionType); // stack: [converterCollection.GetValidationsTree(converterContexts[i], n + i) = validationsTree]
                il.EmitCall(OpCodes.Callvirt, TypeBuilder.GetMethod(collectionType, typeof(IConverterCollection<,>).GetMethod("GetValidationsTree", BindingFlags.Public | BindingFlags.Instance)), null); // stack: [converterCollection.GetValidationsTree(converterContexts[i], n + i) = validationsTree]
                il.Emit(OpCodes.Ldloc, current); // stack: [validationsTree, current]
                //var mutatorsTreeType = typeof(MutatorsTree<>).MakeGenericType(genericParameters[i + 1]);
                //il.Call(mutatorsTreeType.GetMethod("Merge", BindingFlags.Public | BindingFlags.Instance), mutatorsTreeType); // stack: [validationsTree.Merge(current)]
                //il.Call(TypeBuilder.GetMethod(mutatorsTreeType, typeof(MutatorsTree<>).GetMethod("Merge", BindingFlags.Public | BindingFlags.Instance)), mutatorsTreeType); // stack: [validationsTree.Merge(current)]
                il.EmitCall(OpCodes.Callvirt, mergeMethod.MakeGenericMethod(genericParameters[i + 1]), null); // stack: [validationsTree.Merge(current)]

                // Third: Merge with current mutators tree
                il.Emit(OpCodes.Ldarg_1); // stack: [validationsTree.Merge(current), dataConfiguratorCollectionFactory]
                il.EmitCall(OpCodes.Callvirt, getDataConfiguratorCollectionMethod.MakeGenericMethod(genericParameters[i + 1]), null); // stack: [validationsTree.Merge(current), dataConfiguratorCollectionFactory.Get<T_i+1> = collection]

                var collectionIsNullLabel = il.DefineLabel();
                il.Emit(OpCodes.Dup); // stack: [validationsTree.Merge(current), collection, collection]
                il.Emit(OpCodes.Brfalse, collectionIsNullLabel); // if(collection == null) goto collectionIsNull; stack: [validationsTree.Merge(current), collection]

                il.Emit(OpCodes.Ldarg_3); // stack: [validationsTree.Merge(current), collection, mutatorsContexts]
                il.Emit(OpCodes.Ldc_I4, i + 1); // stack: [validationsTree.Merge(current), collection, mutatorsContexts, i + 1]
                il.Emit(OpCodes.Ldelem, typeof(MutatorsContext)); // stack: [validationsTree.Merge(current), collection, mutatorsContexts[i + 1]]
                il.Emit(OpCodes.Ldc_I4, i + 1); // stack: [validationsTree.Merge(current), collection, mutatorsContexts[i + 1], 0]
                // todo ich: избавиться от константы
                collectionType = typeof(IDataConfiguratorCollection<>).MakeGenericType(genericParameters[i + 1]);
                il.EmitCall(OpCodes.Callvirt, TypeBuilder.GetMethod(collectionType, typeof(IDataConfiguratorCollection<>).GetMethod("GetMutatorsTree", new[] {typeof(MutatorsContext), typeof(int)})), null); // stack: [validationsTree.Merge(current), collection.GetMutatorsTree(mutatorsContexts[i + 1], 0)]

                il.MarkLabel(collectionIsNullLabel);

                //il.Call(TypeBuilder.GetMethod(mutatorsTreeType, typeof(MutatorsTree<>).GetMethod("Merge", BindingFlags.Public | BindingFlags.Instance)), mutatorsTreeType); // stack: [validationsTree.Merge(current).Merge(collection.GetMutatorsTree(mutatorsContexts[i + 1], 0)) = current]
                il.EmitCall(OpCodes.Callvirt, mergeMethod.MakeGenericMethod(genericParameters[i + 1]), null); // stack: [validationsTree.Merge(current).Merge(collection.GetMutatorsTree(mutatorsContexts[i + 1], 0)) = current]
            }
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, method);
            typeBuilder.AddInterfaceImplementation(interfaceType);
            return typeBuilder.CreateType();
        }

        private static readonly string getMutatorsTreeMethodName = ((MethodCallExpression)((Expression<Func<IMutatorsTreeCreator<int>, MutatorsTree<int>>>)(creator => creator.GetMutatorsTree(null, null, null, null))).Body).Method.Name;
        private static readonly MethodInfo getDataConfiguratorCollectionMethod = ((MethodCallExpression)((Expression<Func<IDataConfiguratorCollectionFactory, IDataConfiguratorCollection<int>>>)(factory => factory.Get<int>())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo getConverterCollectionMethod = ((MethodCallExpression)((Expression<Func<IConverterCollectionFactory, IConverterCollection<int, int>>>)(factory => factory.Get<int, int>())).Body).Method.GetGenericMethodDefinition();
        private static readonly MethodInfo mergeMethod = ((MethodCallExpression)((Expression<Func<MutatorsTree<int>, MutatorsTree<int>, MutatorsTree<int>>>)((first, second) => Merge(first, second))).Body).Method.GetGenericMethodDefinition();

        private static readonly AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder module = assembly.DefineDynamicModule(Guid.NewGuid().ToString());

        private static readonly object lockObject = new object();

        private static readonly Hashtable hashtable = new Hashtable();
    }
}