namespace GrobExp.Mutators.MultiLanguages
{
    public abstract class MultiLanguageTextBaseWithPath : MultiLanguageTextBase
    {
        public MultiLanguagePathText Path { get; set; }

        public object Value
        {
            get { return value; }
            set
            {
                if(!valueInitialized)
                {
                    valueInitialized = true;
                    this.value = value;
                }
            }
        }

        private object value;
        private bool valueInitialized;
    }
}