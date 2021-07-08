using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Pipeline;
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
                pipeConfigurator.UseDependencyResolver(resolverAccessor);
                
                configurePipeline?.Invoke(pipeConfigurator);
                
                pipeConfigurator.UseHandleQuery(resolverAccessor);
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

            var contextConstructor = QueryContextCtorFactory.GetOrCreate(queryType, typeof(TResult));
            var context = contextConstructor.Construct(query, queryHandlerType, cancellationToken);

            await _queryPipe.Send(context);
            return ((Task<TResult>) context.ResultTask).Result;
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("queryService");

            _queryPipe.Probe(scope.CreateScope("queryPipe"));
        }

        internal static class QueryContextCtorFactory
        {
            private static readonly ConcurrentDictionary<Type, QueryContextConstructor>
                ContextConstructorsCache =
                    new ConcurrentDictionary<Type, QueryContextConstructor>();

            public static QueryContextConstructor GetOrCreate(Type queryType, Type resultType)
            {
                if (!ContextConstructorsCache.TryGetValue(queryType, out var contextConstructor))
                {
                    contextConstructor = QueryContextConstructor.Compile(queryType, resultType);
                    ContextConstructorsCache.TryAdd(queryType, contextConstructor);
                }

                return contextConstructor;
            }
        }

        internal readonly struct QueryContextConstructor
        {
            private readonly Func<IQuery, Type, CancellationToken, IQueryHandlingContext> _ctorInvoker;

            private QueryContextConstructor(
                Type contextType,
                Func<IQuery, Type, CancellationToken, IQueryHandlingContext> ctorInvoker)
            {
                ContextType = contextType;
                _ctorInvoker = ctorInvoker;
            }

            public Type ContextType { get; }

            public IQueryHandlingContext Construct(
                IQuery query, 
                Type handlerType,
                CancellationToken cancellationToken)
            {
                return _ctorInvoker(query, handlerType, cancellationToken);
            }

            public static QueryContextConstructor Compile(Type queryType, Type resultType)
            {
                var contextType = typeof(QueryHandlingContext<,>).MakeGenericType(queryType, resultType);

                return new QueryContextConstructor(contextType, CompileCtorInvoker(queryType, contextType));
            }

            private static Func<IQuery, Type, CancellationToken, IQueryHandlingContext> CompileCtorInvoker(
                Type queryType, 
                Type contextType)
            {
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