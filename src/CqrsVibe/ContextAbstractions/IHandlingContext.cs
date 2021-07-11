using GreenPipes;

namespace CqrsVibe.ContextAbstractions
{
    /// <summary>
    /// Base context
    /// </summary>
    public interface IHandlingContext : PipeContext
    {
        /// <summary>
        /// Resolver to resolve dependencies of filters
        /// </summary>
        IDependencyResolver ContextServices { get; }
    }
}