using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe
{
    internal class HandlerInvokerFactory<TContext>
    {
        private readonly ConcurrentDictionary<Type, HandlerInvoker<TContext>>
            _handlerInvokersCache =
                new ConcurrentDictionary<Type, HandlerInvoker<TContext>>();

        public HandlerInvoker<TContext> GetOrCreate(Type contextType, Type handlerType)
        {
            if (!_handlerInvokersCache.TryGetValue(contextType, out var handlerInvoker))
            {
                handlerInvoker = CreateHandlerInvoker(
                    contextType,
                    handlerType);
                
                _handlerInvokersCache.TryAdd(contextType, handlerInvoker);
            }

            return handlerInvoker;
        }

        private static HandlerInvoker<TContext> CreateHandlerInvoker(
            Type contextType,
            Type handlerInterface)
        {
            var handleMethod = handlerInterface.GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public);
            if (handleMethod == null)
            {
                throw new InvalidOperationException($"{handlerInterface.FullName} does not contain a 'HandleAsync' method");
            }

            var lambdaHandlerParameter = Expression.Parameter(typeof(object));
            var lambdaContextParameter = Expression.Parameter(typeof(TContext));
            var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken));

            var handlerInstance = Expression.Variable(handlerInterface, "handler");
            var concreteContext = Expression.Variable(contextType, "context");

            var block = Expression.Block(
                new[] {handlerInstance, concreteContext},
                Expression.Assign(
                    handlerInstance,
                    Expression.Convert(lambdaHandlerParameter, handlerInterface)),
                Expression.Assign(
                    concreteContext,
                    Expression.Convert(lambdaContextParameter, contextType)),
                Expression.Call(
                    handlerInstance,
                    handleMethod,
                    concreteContext,
                    cancellationTokenParameter));

            var lambda =
                Expression.Lambda<Func<object, TContext, CancellationToken, Task>>(
                    block,
                    lambdaHandlerParameter,
                    lambdaContextParameter,
                    cancellationTokenParameter);

            return new HandlerInvoker<TContext>(handlerInterface, lambda.Compile());
        }
    }
}