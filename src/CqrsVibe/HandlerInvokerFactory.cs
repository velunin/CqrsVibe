using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe
{
    /// <summary>
    /// Factory and cache 'HandleAsync' invokers
    /// </summary>
    /// <typeparam name="TContext">Context type</typeparam>
    internal static class HandlerInvokerFactory<TContext>
    {
        private static readonly ConcurrentDictionary<Type, HandlerInvoker<TContext>>
            HandlerInvokersCache =
                new ConcurrentDictionary<Type, HandlerInvoker<TContext>>();

        public static HandlerInvoker<TContext> GetOrCreate(Type contextType, Type handlerType)
        {
            if (!HandlerInvokersCache.TryGetValue(contextType, out var handlerInvoker))
            {
                handlerInvoker = CreateHandlerInvoker(
                    contextType,
                    handlerType);
                
                HandlerInvokersCache.TryAdd(contextType, handlerInvoker);
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

            var lambda =
                Expression.Lambda<Func<object, TContext, CancellationToken, Task>>(
                    Expression.Call(
                        Expression.Convert(lambdaHandlerParameter, handlerInterface),
                        handleMethod,
                        Expression.Convert(lambdaContextParameter, contextType),
                        cancellationTokenParameter),
                    lambdaHandlerParameter,
                    lambdaContextParameter,
                    cancellationTokenParameter);

            return new HandlerInvoker<TContext>(handlerInterface, lambda.Compile());
        }
    }
}