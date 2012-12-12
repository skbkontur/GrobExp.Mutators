using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp
{
    public class Closure
    {
        public Delegate[] delegates;

        public static readonly FieldInfo DelegatesField = (FieldInfo)((MemberExpression)((Expression<Func<Closure, Delegate[]>>)(closure => closure.delegates)).Body).Member;
    }
}