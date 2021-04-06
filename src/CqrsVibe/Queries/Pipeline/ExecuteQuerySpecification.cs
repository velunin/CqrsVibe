using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;
using GreenPipes.Internals.Extensions;

namespace CqrsVibe.Queries.Pipeline
{
    internal class ExecuteQuerySpecification : IPipeSpecification<IQueryContext>
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly HandlerInvokerFactory<IQueryContext> _queryHandlerInvokerFactory;

        public ExecuteQuerySpecification(IHandlerResolver handlerResolver)
        {
            _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
            _queryHandlerInvokerFactory = new HandlerInvokerFactory<IQueryContext>();
        }

        public void Apply(IPipeBuilder<IQueryContext> builder)
        {
            builder.AddFilter(new DelegateFilter<IQueryContext>(context =>
            {
                var queryContext = (QueryContext) context;
      
                var queryHandlerInvoker = _queryHandlerInvokerFactory.GetOrCreate(
                    queryContext.GetType(), 
                    queryContext.QueryHandlerType);

                var queryHandlerInstance = _handlerResolver.ResolveHandler(queryHandlerInvoker.HandlerType);
                
                queryContext.Result = queryHandlerInvoker.HandleAsync(
                    queryHandlerInstance,
                    context,
                    context.CancellationToken);
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
    
        internal class ConcreteQueryContextConverterFactory : IPipeContextConverterFactory<IQueryContext>
    {
        public IPipeContextConverter<IQueryContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            var queryType = typeof(TOutput).GetClosingArguments(typeof(IQueryContext<>)).Single();
            
            return (IPipeContextConverter<IQueryContext, TOutput>)Activator
                .CreateInstance(typeof(QueryContextConverter<>)
                    .MakeGenericType(queryType));
        }

        private class QueryContextConverter<T> : 
            IPipeContextConverter<IQueryContext, IQueryContext<T>>
            where T : IQuery
        {
            public bool TryConvert(IQueryContext input, out IQueryContext<T> output)
            {
                output = input as IQueryContext<T>;
                return output != null;
            }
        }
    }

    internal class QueryContextConverterFactory : IPipeContextConverterFactory<IQueryContext>
    {
        private readonly Func<IQuery, bool> _filter;

        public QueryContextConverterFactory(Func<IQuery, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public IPipeContextConverter<IQueryContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            return (IPipeContextConverter<IQueryContext, TOutput>)new QueryContextConverter(_filter);
        }
        
        private class QueryContextConverter : IPipeContextConverter<IQueryContext, IQueryContext>
        {
            private readonly Func<IQuery, bool> _filter;

            public QueryContextConverter(Func<IQuery, bool> filter)
            {
                _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }

            public bool TryConvert(IQueryContext input, out IQueryContext output)
            {
                if (!_filter(input.Query))
                {
                    output = null;
                    return false;
                }
                output = input;
                return true;
            }
        }
    }
}