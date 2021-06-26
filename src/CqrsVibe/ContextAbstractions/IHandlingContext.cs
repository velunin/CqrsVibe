using GreenPipes;

namespace CqrsVibe.ContextAbstractions
{
    public interface IHandlingContext : PipeContext
    {
        IDependencyResolver ContextServices { get; }
    }
}