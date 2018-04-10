using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.ModelConfiguration
{
    [DebuggerDisplay("{Value}")]
    public class ModelConfigurationEdge
    {
        public ModelConfigurationEdge(object value)
        {
            Value = value;
        }

        public bool IsArrayIndex { get { return Value is int; } }
        public bool IsMemberAccess { get { return Value is PropertyInfo || Value is FieldInfo; } }
        public bool IsEachMethod { get { return ReferenceEquals(Value, MutatorsHelperFunctions.EachMethod); } }
        public bool IsConvertation { get { return Value is Type; } }
        public bool IsIndexerParams { get { return Value is object[]; } }

        public override int GetHashCode()
        {
            return GetHashCode(Value);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this, obj))
                return true;
            if(ReferenceEquals(this, null) || ReferenceEquals(obj, null))
                return ReferenceEquals(this, null) && ReferenceEquals(obj, null);
            var other = obj as ModelConfigurationEdge;
            if(ReferenceEquals(other, null))
                return false;
            return Compare(Value, other.Value);
        }

        public object Value { get; private set; }

        private static int GetHashCode(object value)
        {
            var arr = value as object[];
            if(arr == null)
            {
                if(value is int)
                    return (int)value;
                if(value is string)
                    return value.GetHashCode();
                var type = value as Type;
                if(type != null)
                    return type.GetHashCode();
                var memberInfo = (MemberInfo)value;
                unchecked
                {
                    return memberInfo.Module.MetadataToken * 397 + memberInfo.MetadataToken;
                }
            }
            var result = 0;
            foreach(var item in arr)
            {
                unchecked
                {
                    result = result * 397 + GetHashCode(item);
                }
            }
            return result;
        }

        private static bool Compare(object left, object right)
        {
            var leftAsArray = left as object[];
            var rightAsArray = right as object[];
            if(leftAsArray != null || rightAsArray != null)
            {
                if(leftAsArray == null || rightAsArray == null)
                    return false;
                if(leftAsArray.Length != rightAsArray.Length)
                    return false;
                return !leftAsArray.Where((t, i) => !Compare(t, rightAsArray[i])).Any();
            }
            if(left is int || right is int)
                return left is int && right is int && (int)left == (int)right;
            if(left is string || right is string)
                return left is string && right is string && (string)left == (string)right;
            if(left is Type || right is Type)
            {
                if(!(left is Type && right is Type))
                    return false;
                return (Type)left == (Type)right;
            }
            if(((MemberInfo)left).Module != ((MemberInfo)right).Module)
                return false;
            return ((MemberInfo)left).MetadataToken == ((MemberInfo)right).MetadataToken;
        }

        public static readonly PropertyInfo ArrayLengthProperty = (PropertyInfo)((MemberExpression)((Expression<Func<Array, int>>)(arr => arr.Length)).Body).Member;

        public static readonly ModelConfigurationEdge Each = new ModelConfigurationEdge(MutatorsHelperFunctions.EachMethod);
        public static readonly ModelConfigurationEdge ArrayLength = new ModelConfigurationEdge(ArrayLengthProperty);
    }
}