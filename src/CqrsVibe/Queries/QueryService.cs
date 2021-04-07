using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.Queries
{
    public class QueryService : IQueryService
    {
        private readonly IPipe<QueryHandlingContext> _queryPipe;
        
        private readonly ConcurrentDictionary<Type, Type> _queryHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        public QueryService(
            IHandlerResolver handlerResolver,
            Action<IPipeConfigurator<IQueryHandlingContext>> configurePipeline = null)
        {
            if (handlerResolver == null)
            {
                throw new ArgumentNullException(nameof(handlerResolver));
            }
            
            _queryPipe = Pipe.New<IQueryHandlingContext>(pipeConfigurator =>
            {
                configurePipeline?.Invoke(pipeConfigurator);
                
                pipeConfigurator.AddPipeSpecification(new HandleQuerySpecification(handlerResolver));
            });
        }

        public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var queryType = query.GetType();
            
            if (!_queryHandlerTypesCache.TryGetValue(queryType, out var queryHandlerType))
            {
                queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
                _queryHandlerTypesCache.TryAdd(queryType, queryHandlerType);
            }
            
            var context = QueryContextFactory.Create(query, queryHandlerType, cancellationToken);

            return _queryPipe
                .Send(context)
                .ContinueWith(
                    sendTask =>
                    {
                        if (sendTask.IsFaulted && sendTask.Exception != null)
                        {
                            throw sendTask.Exception.GetBaseException();
                        }

                        return ((Task<TResult>) context.Result).Result;
                    }, cancellationToken);
        }
        
        internal static class QueryContextFactory
        {
            private static readonly ConcurrentDictionary<Type, Func<IQuery, Type, CancellationToken, QueryHandlingContext>>
                ContextConstructorInvokers =
                    new ConcurrentDictionary<Type, Func<IQuery, Type, CancellationToken, QueryHandlingContext>>();
            
            public static QueryHandlingContext Create(
                IQuery query, 
                Type handlerType,
                CancellationToken cancellationToken)
            {
                var queryType = query.GetType();

                if (!ContextConstructorInvokers.TryGetValue(queryType, out var contextConstructorInvoker))
                {
                    contextConstructorInvoker = CreateContextConstructorInvoker(queryType);
                    ContextConstructorInvokers.TryAdd(queryType, contextConstructorInvoker);
                }

                return contextConstructorInvoker(query, handlerType, cancellationToken);
            }

            private static Func<IQuery,Type,CancellationToken,QueryHandlingContext> CreateContextConstructorInvoker(Type queryType)
            {
                var contextType = typeof(QueryHandlingContext<>).MakeGenericType(queryType);
                var contextConstructorInfo = contextType.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] {queryType, typeof(Type), typeof(CancellationToken)},
                    null);
                
                var queryParameter = Expression.Parameter(typeof(IQuery), "query");
                var handlerParameter = Expression.Parameter(typeof(Type), "handler");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                var concreteQueryInstance = Expression.Variable(queryType, "concreteQuery");

                var block = Expression.Block(new[] {concreteQueryInstance},
                    Expression.Assign(concreteQueryInstance, Expression.Convert(queryParameter, queryType)),
                    Expression.New(contextConstructorInfo!, concreteQueryInstance, handlerParameter, cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<IQuery, Type, CancellationToken, QueryHandlingContext>>(
                        block, queryParameter, handlerParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}