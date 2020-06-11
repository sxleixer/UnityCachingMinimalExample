using ModuleInterface;

namespace Module
{
    
    public class Module : IModule
    {
        private readonly ILogger _logger;
        private readonly BadException _exception;
        private readonly Dependency _dependency;

        public Module(ILogger logger, BadException exception, Dependency dependency)
        {
            _logger = logger;
            _exception = exception;
            _dependency = dependency;
        }

        public void WriteInformation()
        {
            _logger.Log("Hello Host, nice to meet you! :)");
            _logger.Log(_exception);
            _logger.Log($"{_dependency}");
        }
        
        
    }
}