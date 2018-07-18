namespace GrobExp.Mutators
{
    public abstract class MutatorsContext
    {
        public abstract string GetKey();
        public static readonly MutatorsContext Empty = new EmptyMutatorsContext();
    }

    public sealed class EmptyMutatorsContext : MutatorsContext
    {
        public override string GetKey()
        {
            return "";
        }
    }
}