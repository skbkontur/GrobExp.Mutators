namespace GrobExp.Mutators.AutoEvaluators
{
    public static class EqualsToConfigurationExtensions
    {
        public static bool IsUncoditionalSetter(this MutatorConfiguration mutator)
        {
            return mutator is EqualsToConfiguration && !(mutator is EqualsToIfConfiguration);
        }
    }
}