using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    internal class ConfiguratorReporter
    {
        public void Report(Expression simplifiedPath, Expression path, MutatorConfiguration mutator)
        {
            SimplifiedPath = simplifiedPath;
            Path = path;
            Mutator = mutator;
        }

        public Expression SimplifiedPath { get; private set; }
        
        public Expression Path { get; private set; }
        
        public MutatorConfiguration Mutator { get; private set; }
    }
}