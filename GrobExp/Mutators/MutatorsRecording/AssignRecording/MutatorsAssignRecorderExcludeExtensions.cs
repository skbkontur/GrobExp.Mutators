using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public static class MutatorsAssignRecorderExcludeExtensions
    {
        public static IMutatorsAssignRecorder ExcludingType(this IMutatorsAssignRecorder recorder, Type typeToExclude)
        {
            recorder.ExcludeFromCoverage(x => x.Type == typeToExclude);
            return recorder;
        }

        public static IMutatorsAssignRecorder ExcludingType<T>(this IMutatorsAssignRecorder recorder)
        {
            return recorder.ExcludingType(typeof(T));
        }

        public static IMutatorsAssignRecorder ExcludingInterface(this IMutatorsAssignRecorder recorder, Type interfaceTypeToExclude)
        {
            recorder.ExcludeFromCoverage(x => interfaceTypeToExclude.IsAssignableFrom(x.Type));
            return recorder;
        }

        public static IMutatorsAssignRecorder ExcludingInterface<T>(this IMutatorsAssignRecorder recorder)
        {
            return recorder.ExcludingInterface(typeof(T));
        }

        public static IMutatorsAssignRecorder ExcludingProperty<TType, TProperty>(this IMutatorsAssignRecorder recorder, Expression<Func<TType, TProperty>> propertyExpression)
        {
            var propertyInfo = (PropertyInfo)((MemberExpression)propertyExpression.Body).Member;
            recorder.ExcludeFromCoverage(x => AreEqualProperties<TType>(propertyInfo, x));
            return recorder;
        }

        private static bool AreEqualProperties<TDeclaringType>(PropertyInfo expectedProperty, Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            if(memberExpression == null) return false;
            var actualProperty = memberExpression.Member as PropertyInfo;
            if(actualProperty == null || actualProperty.DeclaringType == null) return false;
            return expectedProperty.Name == actualProperty.Name && typeof(TDeclaringType).IsAssignableFrom(actualProperty.DeclaringType);
        }

        public static IMutatorsAssignRecorder ExcludingGenericProperty<TGenericType, TProperty>(this IMutatorsAssignRecorder recorder, Expression<Func<TGenericType, TProperty>> propertyExpression)
        {
            var propertyInfo = (PropertyInfo)((MemberExpression)propertyExpression.Body).Member;
            recorder.ExcludeFromCoverage(x => AreEqualGenericProperties<TGenericType>(propertyInfo, x));
            return recorder;
        }

        private static bool AreEqualGenericProperties<TDeclaringType>(PropertyInfo expectedProperty, Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            if(memberExpression == null) return false;
            var actualProperty = memberExpression.Member as PropertyInfo;
            if(actualProperty == null || actualProperty.DeclaringType == null) return false;
            var declaringType = typeof(TDeclaringType).TryGetGenericTypeDefinition();
            if(actualProperty.DeclaringType.GetInterfaces().All(x => x.TryGetGenericTypeDefinition() != declaringType)) return false;
            return expectedProperty.Name == actualProperty.Name;
        }

        private static Type TryGetGenericTypeDefinition(this Type type)
        {
            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }
    }
}