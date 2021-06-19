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
        private readonly IPipe<IQueryHandlingContext> _queryPipe;
        
        private readonly ConcurrentDictionary<Type, Type> _queryHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        public QueryService(
            IDependencyResolverAccessor resolverAccessor,
            Action<IPipeConfigurator<IQueryHandlingContext>> configurePipeline = null)
        {
            if (resolverAccessor == null)
            {
                throw new ArgumentNullException(nameof(resolverAccessor));
            }
            
            _queryPipe = Pipe.New<IQueryHandlingContext>(pipeConfigurator =>
            {
                pipeConfigurator.AddPipeSpecification(new SetDependencyResolverSpecification<IQueryHandlingContext>(resolverAccessor));
                
                configurePipeline?.Invoke(pipeConfigurator);
                
                pipeConfigurator.AddPipeSpecification(new HandleQuerySpecification(resolverAccessor));
            });
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
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

            await _queryPipe.Send(context);
            return ((Task<TResult>) context.Result).Result;
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
                var handlerInterfaceParameter = Expression.Parameter(typeof(Type), "handlerInterface");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                var concreteQueryInstance = Expression.Variable(queryType, "concreteQuery");

                var block = Expression.Block(new[] {concreteQueryInstance},
                    Expression.Assign(concreteQueryInstance, Expression.Convert(queryParameter, queryType)),
                    Expression.New(contextConstructorInfo!, concreteQueryInstance, handlerInterfaceParameter, cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<IQuery, Type, CancellationToken, QueryHandlingContext>>(
                        block, queryParameter, handlerInterfaceParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}