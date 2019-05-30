using System;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace GrobExp.Mutators.ModelConfiguration.Traverse
{
    internal static class ModelConfigurationNodeExtensions
    {
        public static ModelConfigurationNode GotoEachArrayElement(this ModelConfigurationNode node, bool create)
        {
            return node.GetChild(ModelConfigurationEdge.Each, node.NodeType.GetItemType(), create);
        }

        public static ModelConfigurationNode GotoMember([NotNull] this ModelConfigurationNode node, [NotNull] MemberInfo member, bool create)
        {
            var edge = new ModelConfigurationEdge(member);
            switch (member)
            {
                case FieldInfo fieldInfo:
                    return node.GetChild(edge, fieldInfo.FieldType, create);
                case PropertyInfo propertyInfo:
                    return node.GetChild(edge, propertyInfo.PropertyType, create);
                default:
                    throw new NotSupportedException("Member rootType " + member.MemberType + " is not supported");
            }
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoTypeConversion([NotNull] this ModelConfigurationNode node, [NotNull] Type type, bool create)
        {
            var edge = new ModelConfigurationEdge(type);

            return node.GetChild(edge, type, create);
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoIndexer([NotNull] this ModelConfigurationNode node, [NotNull] object[] parameters, bool create)
        {
            var edge = new ModelConfigurationEdge(parameters);
            var property = node.NodeType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException("Type '" + node.NodeType + "' doesn't contain indexer");

            if (!property.CanRead || !property.CanWrite)
                throw new InvalidOperationException("Type '" + node.NodeType + "' has indexer that doesn't contain either getter or setter");

            return node.GetChild(edge, property.GetGetMethod().ReturnType, create);
        }

        [CanBeNull]
        public static ModelConfigurationNode GotoArrayElement([NotNull] this ModelConfigurationNode node, int index, bool create)
        {
            return node.GetChild(new ModelConfigurationEdge(index), node.NodeType.GetItemType(), create);
        }

        [CanBeNull]
        private static ModelConfigurationNode GetChild([NotNull] this ModelConfigurationNode node, [NotNull] ModelConfigurationEdge edge, [NotNull] Type childType, bool create)
        {
            if (!node.children.TryGetValue(edge, out var child) && create)
            {
                Expression path;
                if (edge.IsArrayIndex)
                    path = node.Path.MakeArrayIndex((int)edge.Value);
                else if (edge.IsMemberAccess)
                    path = node.Path.MakeMemberAccess((MemberInfo)edge.Value);
                else if (edge.IsEachMethod)
                    path = node.Path.MakeEachCall(childType);
                else if (edge.IsConvertation)
                    path = node.Path.MakeConvertation((Type)edge.Value);
                else if (edge.IsIndexerParams)
                    path = node.Path.MakeIndexerCall((object[])edge.Value, node.NodeType);
                else throw new InvalidOperationException();

                child = new ModelConfigurationNode(node.ConfiguratorType, node.RootType, childType, node.Root, node, edge, path);
                node.children.Add(edge, child);
            }

            return child;
        }
    }
}