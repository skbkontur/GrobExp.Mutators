using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    internal class MutatorsAssignRecorder : IMutatorsAssignRecorder
    {
        public MutatorsAssignRecorder(Type[] typesToExclude, PropertyInfo[] propertiesToExclude)
        {
            recordsCollection = new AssignRecordCollection();
            this.typesToExclude = typesToExclude;
            this.propertiesToExclude = propertiesToExclude;
        }

        public List<RecordNode> GetRecords()
        {
            return recordsCollection.GetRecords();
        }

        public void Stop()
        {
            instance = null;
        }

        public static MutatorsAssignRecorder StartRecording(Type[] excludedFromCoverage, PropertyInfo[] propertiesToExclude)
        {
            return instance ?? (instance = new MutatorsAssignRecorder(excludedFromCoverage ?? new Type[0], propertiesToExclude ?? new PropertyInfo[0]));
        }

        public static void RecordCompilingExpression(AssignLogInfo toLog)
        {
            var isExcluded = IsExcludedFromCoverage(toLog);
            instance.recordsCollection.RecordCompilingExpression(toLog.Path.ToString(), toLog.Value.ToString(), isExcluded);
        }

        private static bool IsExcludedFromCoverage(AssignLogInfo toLog)
        {
            var propsToExclude = GetInterfacePropertiesToExclude(toLog.Path).Union(instance.propertiesToExclude);
            var isExcludedByProperty = toLog.Path.SmashToSmithereens().OfType<MemberExpression>().Any(x => propsToExclude.Contains(x.Member));
            var chainsOfPath = toLog.Path.SmashToSmithereens().Select(x => x.Type).ToArray();
            var chainsOfValue = toLog.Value.SmashToSmithereens().Select(x => x.Type).ToArray();
            var isExcluded = isExcludedByProperty || instance.typesToExclude.Any(x => chainsOfPath.Contains(x) || chainsOfValue.Contains(x));
            return isExcluded;
        }

        public static void RecordExecutingExpression(AssignLogInfo toLog)
        {
            if(IsRecording())
            {
                var isExcluded = new Lazy<bool>(() => IsExcludedFromCoverage(toLog));
                instance.recordsCollection.RecordExecutingExpression(toLog.Path.ToString(), toLog.Value.ToString(), isExcluded);
            }
        }

        public static void RecordExecutingExpressionWithValueObjectCheck(AssignLogInfo toLog, object executedValue)
        {
            if(executedValue != null)
                RecordExecutingExpression(toLog);
        }

        public static void RecordExecutingExpressionWithNullableValueCheck<T>(AssignLogInfo toLog, T? executedValue) where T : struct 
        {
            if (executedValue != null || toLog.Value.NodeType == ExpressionType.Constant && toLog.Value.ToConstant().Value == null)
                RecordExecutingExpression(toLog);
        }

        public static void RecordConverter(string converter)
        {
            instance.recordsCollection.AddConverterToRecord(converter);
        }

        public static bool IsRecording()
        {
            return instance != null;
        }

        public static void StopRecordingConverter()
        {
            instance.recordsCollection.ResetCurrentConvertor();
        }

        private static IEnumerable<PropertyInfo> GetInterfacePropertiesToExclude(Expression exp)
        {
            var props = new List<PropertyInfo>();
            foreach(var property in instance.propertiesToExclude.Where(x => x.DeclaringType != null && x.DeclaringType.IsInterface))
            {
                props.AddRange(exp.SmashToSmithereens()
                                  .Where(x => x.Type.GetInterfaces().Select(y => y.IsGenericType ? y.GetGenericTypeDefinition() : y).Contains(property.DeclaringType))
                                  .Select(x => x.Type.GetInterfaceMap(x.Type.GetInterface(property.DeclaringType.Name)))
                                  .SelectMany(x => x.TargetType.GetProperties().Where(y => y.Name == property.Name)));
            }

            return props;
        }

        [ThreadStatic]
        private static MutatorsAssignRecorder instance;
        private readonly AssignRecordCollection recordsCollection;
        private readonly Type[] typesToExclude;
        private readonly PropertyInfo[] propertiesToExclude;
    }
}