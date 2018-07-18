using System;
using System.Collections;
using System.Linq.Expressions;

using GrobExp.Compiler;
using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.MultiLanguages
{
    public abstract class MultiLanguageTextBase
    {
        public string GetText(string language)
        {
// ReSharper disable IntroduceOptionalParameters.Global
            return GetText(language, Default);
// ReSharper restore IntroduceOptionalParameters.Global
        }

        public string GetText(string language, string context)
        {
            Initialize();
            string key = GetKey(GetType(), language, context);
            if (!functions.ContainsKey(key))
            {
                if (context != Default)
                    return GetText(language, Default);
                throw new UnknownLanguageException(key);
            }

            return ((Func<MultiLanguageTextBase, string>)functions[key])(this);
        }

        public Expression<Func<MultiLanguageTextBase, string>> GetExpression(string language)
        {
// ReSharper disable IntroduceOptionalParameters.Global
            return GetExpression(language, Default);
// ReSharper restore IntroduceOptionalParameters.Global
        }

        public Expression<Func<MultiLanguageTextBase, string>> GetExpression(string language, string context)
        {
            Initialize();
            string key = GetKey(GetType(), language, context);
            if (!expressions.ContainsKey(key))
            {
                if (context != Default)
                    return GetExpression(language, Default);
                throw new UnknownLanguageException(key);
            }

            return (Expression<Func<MultiLanguageTextBase, string>>)expressions[key];
        }

        public const string Default = "default";
        public const string Web = "web";

        protected abstract void Register();

        protected void Register(string language, string context, Expression<Func<string>> textGetter)
        {
            Type type = GetType();
            string key = GetKey(type, language, context);
            var exp = new TextGeneralizer(type).Generalize(textGetter);
            expressions.Add(key, exp);
            functions.Add(key, LambdaCompiler.Compile(exp, CompilerOptions.None));
        }

        protected void Register(string language, Expression<Func<string>> textGetter)
        {
            Register(language, Default, textGetter);
        }

        private void Initialize()
        {
            var type = GetType();
            if (initialized[type] != null) return;
            lock (lockObject)
            {
                if (initialized[type] != null) return;
                Register();
                initialized.Add(type, dummy);
            }
        }

        private static string GetKey(Type type, string language, string context)
        {
            return type + "@" + language + "@" + context;
        }

        private static readonly Hashtable initialized = new Hashtable();
        private static readonly Hashtable expressions = new Hashtable();
        private static readonly Hashtable functions = new Hashtable();
        private static readonly object lockObject = new object();
        private static readonly object dummy = new object();
    }
}