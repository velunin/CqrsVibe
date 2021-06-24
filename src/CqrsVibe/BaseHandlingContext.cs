using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe
{
    public abstract class BaseHandlingContext : BasePipeContext, IHandlingContext
    {
        protected BaseHandlingContext(CancellationToken cancellationToken) : base(cancellationToken)
        {
        }
        
        public IDependencyResolver ContextServices { get; internal set; }
    }

    public interface IHandlingContext : PipeContext
    {
        IDependencyResolver ContextServices { get; }
    }

    public interface IResultingHandlingContext
    {
        Task Result { get; }
        
        void SetResult(Task result);
    }
}