using System.Collections.Generic;
using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    internal class ConfiguratorReporter
    {
        public List<Report> Reports { get; } = new List<Report>();

        public void Report(Expression simplifiedPath, Expression path, MutatorConfiguration mutator)
        {
            Reports.Add(new Report(simplifiedPath, path, mutator));
        }
    }

    internal class Report
    {
        public Report(Expression simplifiedPath, Expression path, MutatorConfiguration mutator)
        {
            SimplifiedPath = simplifiedPath;
            Path = path;
            Mutator = mutator;
        }

        public Expression SimplifiedPath { get; }

        public Expression Path { get; }

        public MutatorConfiguration Mutator { get; }
    }
}