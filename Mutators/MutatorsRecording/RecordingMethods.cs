using System;
using System.Linq.Expressions;
using System.Reflection;

using GrobExp.Mutators.MutatorsRecording.AssignRecording;
using GrobExp.Mutators.MutatorsRecording.ValidationRecording;

namespace GrobExp.Mutators.MutatorsRecording
{
    internal class RecordingMethods
    {
        public static readonly MethodInfo RecordExecutingValidationMethodInfo = ((MethodCallExpression)((Expression<Action<Type, ValidationLogInfo, string>>)((converterType, validationInfo, validationResult) => MutatorsValidationRecorder.RecordExecutingValidation(converterType, validationInfo, validationResult))).Body).Method;
        public static readonly MethodInfo RecordExecutingExpressionMethodInfo = ((MethodCallExpression)((Expression<Action<Type, AssignLogInfo>>)((converterType, toLog) => MutatorsAssignRecorder.RecordExecutingExpression(converterType, toLog))).Body).Method;
        public static readonly MethodInfo RecordExecutingExpressionWithValueObjectCheck = ((MethodCallExpression)((Expression<Action<Type, AssignLogInfo, object>>)((converterType, toLog, executedValue) => MutatorsAssignRecorder.RecordExecutingExpressionWithValueObjectCheck(converterType, toLog, executedValue))).Body).Method;
        public static readonly MethodInfo RecordExecutingExpressionWithNullableValueCheckGenericMethodInfo = ((MethodCallExpression)((Expression<Action<Type, AssignLogInfo, int?>>)((converterType, toLog, expectedValue) => MutatorsAssignRecorder.RecordExecutingExpressionWithNullableValueCheck(converterType, toLog, expectedValue))).Body).Method.GetGenericMethodDefinition();
    }
}