using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators.MutatorsRecording.AssignRecording
{
    public interface IMutatorsAssignRecorder
    {
        List<RecordNode> GetRecords();
        void Stop();
        void ExcludeFromCoverage(Func<Expression, bool> excludeCriterion);
    }
}