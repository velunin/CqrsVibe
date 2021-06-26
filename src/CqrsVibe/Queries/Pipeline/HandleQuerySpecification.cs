using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe.Queries.Pipeline
{
    internal class HandleQuerySpecification : IPipeSpecification<IQueryHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;
        private readonly HandlerInvokerFactory<IQueryHandlingContext> _queryHandlerInvokerFactory;

        public HandleQuerySpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor ?? throw new ArgumentNullException(nameof(resolverAccessor));
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

                var queryHandlerInstance = _resolverAccessor.Current.ResolveService(queryHandlerInvoker.HandlerInterface);

                queryContext.SetResultTask(queryHandlerInvoker.HandleAsync(
                    queryHandlerInstance,
                    context,
                    context.CancellationToken));

                return queryContext.ResultTask;
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}