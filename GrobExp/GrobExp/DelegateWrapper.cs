using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp
{
    public static class DelegateWrapper
    {
        // todo ich: а можно это заинлайнить?
        public static Func<TClosure, Func<TResult>> WrapFunc<TClosure, TResult>(Func<TClosure, TResult> func) where TClosure: Closure
        {
            return closure => (() => func(closure));
        }

        public static Func<TClosure, Func<T, TResult>> WrapFunc<TClosure, T, TResult>(Func<TClosure, T, TResult> func) where TClosure : Closure
        {
            return closure => (arg => func(closure, arg));
        }

        public static Func<TClosure, Func<T1, T2, TResult>> WrapFunc<TClosure, T1, T2, TResult>(Func<TClosure, T1, T2, TResult> func) where TClosure : Closure
        {
            return closure => ((arg1, arg2) => func(closure, arg1, arg2));
        }

        public static Func<TClosure, Func<T1, T2, T3, TResult>> WrapFunc<TClosure, T1, T2, T3, TResult>(Func<TClosure, T1, T2, T3, TResult> func) where TClosure : Closure
        {
            return closure => ((arg1, arg2, arg3) => func(closure, arg1, arg2, arg3));
        }

        public static Func<TClosure, Action> WrapAction<TClosure>(Action<TClosure> action) where TClosure : Closure
        {
            return closure => (() => action(closure));
        }

        public static Func<TClosure, Action<T>> WrapAction<TClosure, T>(Action<TClosure, T> action) where TClosure : Closure
        {
            return closure => (arg => action(closure, arg));
        }

        public static Func<TClosure, Action<T1, T2>> WrapAction<TClosure, T1, T2>(Action<TClosure, T1, T2> action) where TClosure : Closure
        {
            return closure => ((arg1, arg2) => action(closure, arg1, arg2));
        }

        public static Func<TClosure, Action<T1, T2, T3>> WrapAction<TClosure, T1, T2, T3>(Action<TClosure, T1, T2, T3> action) where TClosure : Closure
        {
            return closure => ((arg1, arg2, arg3) => action(closure, arg1, arg2, arg3));
        }

        public static MethodInfo wrapFunc2Method = ((MethodCallExpression)((Expression<Func<Func<Closure, int>, Func<Closure, Func<int>>>>)(func => WrapFunc(func))).Body).Method.GetGenericMethodDefinition();
        public static MethodInfo wrapFunc3Method = ((MethodCallExpression)((Expression<Func<Func<Closure, int, int>, Func<Closure, Func<int, int>>>>)(func => WrapFunc(func))).Body).Method.GetGenericMethodDefinition();
        public static MethodInfo wrapFunc4Method = ((MethodCallExpression)((Expression<Func<Func<Closure, int, int, int>, Func<Closure, Func<int, int, int>>>>)(func => WrapFunc(func))).Body).Method.GetGenericMethodDefinition();
        public static MethodInfo wrapFunc5Method = ((MethodCallExpression)((Expression<Func<Func<Closure, int, int, int, int>, Func<Closure, Func<int, int, int, int>>>>)(func => WrapFunc(func))).Body).Method.GetGenericMethodDefinition();

        public static MethodInfo wrapAction1Method = ((MethodCallExpression)((Expression<Func<Action<Closure>, Func<Closure, Action>>>)(action => WrapAction(action))).Body).Method;
        public static MethodInfo wrapAction2Method = ((MethodCallExpression)((Expression<Func<Action<Closure, int>, Func<Closure, Action<int>>>>)(action => WrapAction(action))).Body).Method.GetGenericMethodDefinition();
        public static MethodInfo wrapAction3Method = ((MethodCallExpression)((Expression<Func<Action<Closure, int, int>, Func<Closure, Action<int, int>>>>)(action => WrapAction(action))).Body).Method.GetGenericMethodDefinition();
        public static MethodInfo wrapAction4Method = ((MethodCallExpression)((Expression<Func<Action<Closure, int, int, int>, Func<Closure, Action<int, int, int>>>>)(action => WrapAction(action))).Body).Method.GetGenericMethodDefinition();
    }
}