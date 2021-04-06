using System;
using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe
{
    internal readonly struct HandlerInvoker<TContext>
    {
        private readonly Func<object, TContext, CancellationToken, Task> _invoker;
        
        public HandlerInvoker(Type handlerType, Func<object, TContext, CancellationToken, Task> invoker)
        {
            HandlerType = handlerType;
            _invoker = invoker;
        }

        public Type HandlerType { get; }

        public Task HandleAsync(object handlerInstance, TContext context, CancellationToken cancellationToken) =>
            _invoker(handlerInstance, context, cancellationToken);
    }
}