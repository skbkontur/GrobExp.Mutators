using System;
using System.Reflection;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public class AssignRecorderInitializer
    {
        public static IMutatorsAssignRecorder StartAssignRecorder(Type[] excludedFromCoverage = null, PropertyInfo[] propertiesToExclude = null)
        {
            return MutatorsAssignRecorder.StartRecording(excludedFromCoverage, propertiesToExclude);
        }
    }
}