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
    /// <summary>
    /// Implementation of <see cref="IQueryService"/> interface
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly IPipe<IQueryHandlingContext> _queryPipe;

        private readonly ConcurrentDictionary<Type, Type> _queryHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryService"/> class.
        /// </summary>
        /// <param name="resolverAccessor">Dependency resolver accessor</param>
        /// <param name="configurePipeline">Delegate for configure query pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolverAccessor"/> is null</exception>
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

        /// <summary>
        /// Executes query
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult">Query result type</typeparam>
        /// <returns>Query result</returns>
        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query,
            CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var queryType = query.GetType();

            var queryHandlerType = _queryHandlerTypesCache.GetOrAdd(
                queryType,
                queryTypeArg => typeof(IQueryHandler<,>).MakeGenericType(queryTypeArg, typeof(TResult)));

            var contextConstructor = QueryContextCtorFactory.GetOrCreate(queryType, typeof(TResult));
            var context = contextConstructor.Construct(query, queryHandlerType, cancellationToken);

            await _queryPipe.Send(context);
            return ((Task<TResult>)context.ResultTask).Result;
        }

        /// <inheritdoc />
        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("queryService");

            _queryPipe.Probe(scope.CreateScope("queryPipe"));
        }

        /// <summary>
        /// Factory and cache for context command constructors
        /// </summary>
        internal static class QueryContextCtorFactory
        {
            private static readonly ConcurrentDictionary<Type, QueryContextConstructor>
                ContextConstructorsCache =
                    new ConcurrentDictionary<Type, QueryContextConstructor>();

            public static QueryContextConstructor GetOrCreate(Type queryType, Type resultType)
            {
                return ContextConstructorsCache.GetOrAdd(queryType,
                    (queryTypeArg, resultTypeArg) => QueryContextConstructor.Compile(queryTypeArg, resultTypeArg),
                    resultType);
            }
        }

        /// <summary>
        /// For invoke constructor of concrete query context in runtime
        /// </summary>
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
                    new[] { queryType, typeof(Type), typeof(CancellationToken) },
                    null);

                var queryParameter = Expression.Parameter(typeof(IQuery), "query");
                var handlerInterfaceParameter = Expression.Parameter(typeof(Type), "handlerInterface");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                var concreteQueryInstance = Expression.Variable(queryType, "concreteQuery");

                var block = Expression.Block(new[] { concreteQueryInstance },
                    Expression.Assign(concreteQueryInstance, Expression.Convert(queryParameter, queryType)),
                    Expression.New(contextConstructorInfo!, concreteQueryInstance, handlerInterfaceParameter,
                        cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<IQuery, Type, CancellationToken, QueryHandlingContext>>(
                        block, queryParameter, handlerInterfaceParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}