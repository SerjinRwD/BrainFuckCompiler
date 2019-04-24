namespace BF
{
    public class TranslatorBuilder
    {
        private int _memorySize;
        private string _outputDirectory;

        public TranslatorBuilder WithMemorySize(int size)
        {
            _memorySize = size;
            return this;
        }

        public TranslatorBuilder SetOutputDirectory(string path)
        {
            _outputDirectory = path;
            return this;
        }

        public Translator Build()
        {
            return new Translator
            {
                MemorySize = _memorySize,
                OutputDirectory = _outputDirectory
            };
        }
    }
}
