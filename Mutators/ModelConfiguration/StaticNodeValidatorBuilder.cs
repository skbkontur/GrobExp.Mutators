using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.Validators;

namespace GrobExp.Mutators.ModelConfiguration
{
    public static class StaticNodeValidatorBuilder
    {
        public static LambdaExpression BuildStaticNodeValidator(this ModelConfigurationNode node)
        {
            var parameter = node.Parent == null ? (ParameterExpression)node.Path : Expression.Parameter(node.NodeType, node.NodeType.Name);
            var result = Expression.Variable(typeof(List<ValidationResult>), "result");
            Expression initResult = Expression.Assign(result, Expression.New(listValidationResultConstructor));
            var validationResults = new List<Expression> {initResult};
            foreach (var mutator in node.Mutators.Where(mutator => mutator.Value is ValidatorConfiguration))
            {
                var validator = (ValidatorConfiguration)mutator.Value;
                var ok = true;
                foreach (var dependency in validator.Dependencies ?? new LambdaExpression[0])
                {
                    ModelConfigurationNode child;
                    if (!node.Root.Traverse(dependency.Body, node, out child, false) || child != node)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    var current = validator.Apply(node.ConverterType, new List<KeyValuePair<Expression, Expression>> {new KeyValuePair<Expression, Expression>(parameter, node.Path)});
                    if (current != null)
                        validationResults.Add(Expression.Call(result, listAddValidationResultMethod, current));
                }
            }

            validationResults.Add(result);
            Expression body = Expression.Block(new[] {result}, validationResults);
            return Expression.Lambda(body, parameter);
        }

        private static readonly MethodInfo listAddValidationResultMethod = ((MethodCallExpression)((Expression<Action<List<ValidationResult>>>)(list => list.Add(null))).Body).Method;
        private static readonly ConstructorInfo listValidationResultConstructor = ((NewExpression)((Expression<Func<List<ValidationResult>>>)(() => new List<ValidationResult>())).Body).Constructor;
    }
}