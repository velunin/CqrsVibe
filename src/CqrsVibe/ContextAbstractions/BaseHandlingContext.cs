using System.Threading;
using GreenPipes;

namespace CqrsVibe.ContextAbstractions
{
    public abstract class BaseHandlingContext : BasePipeContext, IHandlingContext
    {
        protected BaseHandlingContext(CancellationToken cancellationToken) : base(cancellationToken)
        {
        }

        public IDependencyResolver ContextServices { get; internal set; }
    }
}