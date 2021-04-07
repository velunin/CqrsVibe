using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;
using GreenPipes.Internals.Extensions;

namespace CqrsVibe.Queries.Pipeline
{
    internal class HandleQuerySpecification : IPipeSpecification<IQueryHandlingContext>
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly HandlerInvokerFactory<IQueryHandlingContext> _queryHandlerInvokerFactory;

        public HandleQuerySpecification(IHandlerResolver handlerResolver)
        {
            _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
            _queryHandlerInvokerFactory = new HandlerInvokerFactory<IQueryHandlingContext>();
        }

        public void Apply(IPipeBuilder<IQueryHandlingContext> builder)
        {
            builder.AddFilter(new InlineFilter<IQueryHandlingContext>((context, next) =>
            {
                var queryContext = (QueryHandlingContext) context;
      
                var queryHandlerInvoker = _queryHandlerInvokerFactory.GetOrCreate(
                    queryContext.GetType(), 
                    queryContext.QueryHandlerInterface);

                var queryHandlerInstance = _handlerResolver.ResolveHandler(queryHandlerInvoker.HandlerInterface);
                
                return queryContext.Result = queryHandlerInvoker.HandleAsync(
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
    
    internal class ConcreteQueryContextConverterFactory : IPipeContextConverterFactory<IQueryHandlingContext>
    {
        public IPipeContextConverter<IQueryHandlingContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            var queryType = typeof(TOutput).GetClosingArguments(typeof(IQueryHandlingContext<>)).Single();
            
            return (IPipeContextConverter<IQueryHandlingContext, TOutput>)Activator
                .CreateInstance(typeof(QueryContextConverter<>)
                    .MakeGenericType(queryType));
        }

        private class QueryContextConverter<T> : 
            IPipeContextConverter<IQueryHandlingContext, IQueryHandlingContext<T>>
            where T : IQuery
        {
            public bool TryConvert(IQueryHandlingContext input, out IQueryHandlingContext<T> output)
            {
                output = input as IQueryHandlingContext<T>;
                return output != null;
            }
        }
    }

    internal class QueryContextConverterFactory : IPipeContextConverterFactory<IQueryHandlingContext>
    {
        private readonly Func<IQuery, bool> _filter;

        public QueryContextConverterFactory(Func<IQuery, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public IPipeContextConverter<IQueryHandlingContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            return (IPipeContextConverter<IQueryHandlingContext, TOutput>)new QueryContextConverter(_filter);
        }
        
        private class QueryContextConverter : IPipeContextConverter<IQueryHandlingContext, IQueryHandlingContext>
        {
            private readonly Func<IQuery, bool> _filter;

            public QueryContextConverter(Func<IQuery, bool> filter)
            {
                _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }

            public bool TryConvert(IQueryHandlingContext input, out IQueryHandlingContext output)
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