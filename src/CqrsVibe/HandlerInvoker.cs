using System;
using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe
{
    /// <summary>
    /// Invokes 'HandleAsync' method of handlers
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    internal readonly struct HandlerInvoker<TContext>
    {
        private readonly Func<object, TContext, CancellationToken, Task> _invoker;

        /// <summary>
        /// Initialize of <see cref="HandlerInvoker{TContext}"/>
        /// </summary>
        /// <param name="handlerInterface">Type of handler interface</param>
        /// <param name="invoker">Compiled lambda for call 'HandleAsync' method of the handler</param>
        public HandlerInvoker(Type handlerInterface, Func<object, TContext, CancellationToken, Task> invoker)
        {
            HandlerInterface = handlerInterface;
            _invoker = invoker;
        }

        /// <summary>
        /// Type of handler interface
        /// </summary>
        public Type HandlerInterface { get; }

        /// <summary>
        /// Invoke 'HandleAsync' method of <paramref name="handlerInstance"/>
        /// </summary>
        /// <param name="handlerInstance">Instance of handler</param>
        /// <param name="context">Handling context</param>
        /// <param name="cancellationToken"></param>
        public Task HandleAsync(object handlerInstance, TContext context, CancellationToken cancellationToken) =>
            _invoker(handlerInstance, context, cancellationToken);
    }
}