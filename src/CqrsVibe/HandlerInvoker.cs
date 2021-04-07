using System;
using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe
{
    internal readonly struct HandlerInvoker<TContext>
    {
        private readonly Func<object, TContext, CancellationToken, Task> _invoker;
        
        public HandlerInvoker(Type handlerInterface, Func<object, TContext, CancellationToken, Task> invoker)
        {
            HandlerInterface = handlerInterface;
            _invoker = invoker;
        }

        public Type HandlerInterface { get; }

        public Task HandleAsync(object handlerInstance, TContext context, CancellationToken cancellationToken) =>
            _invoker(handlerInstance, context, cancellationToken);
    }
}