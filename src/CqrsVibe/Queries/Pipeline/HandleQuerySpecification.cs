using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Queries.Pipeline
{
    /// <summary>
    /// Specification for add <see cref="HandleQueryFilter"/> to pipeline
    /// </summary>
    internal class HandleQuerySpecification : IPipeSpecification<IQueryHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleQuerySpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor ?? throw new ArgumentNullException(nameof(resolverAccessor));
        }

        public void Apply(IPipeBuilder<IQueryHandlingContext> builder)
        {
            builder.AddFilter(new HandleQueryFilter(_resolverAccessor));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    /// <summary>
    /// Filter for resolving and invoking query handlers
    /// </summary>
    internal class HandleQueryFilter : IFilter<IQueryHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleQueryFilter(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor;
        }

        public Task Send(IQueryHandlingContext context, IPipe<IQueryHandlingContext> next)
        {
            var queryContext = (QueryHandlingContext) context;
      
            var queryHandlerInvoker = HandlerInvokerFactory<IQueryHandlingContext>.GetOrCreate(
                queryContext.GetType(), 
                queryContext.QueryHandlerInterface);

            var queryHandlerInstance = _resolverAccessor.Current.ResolveService(queryHandlerInvoker.HandlerInterface);

            queryContext.SetResultTask(queryHandlerInvoker.HandleAsync(
                queryHandlerInstance,
                context,
                context.CancellationToken));

            return queryContext.ResultTask;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("handleQuery");
        }
    }
}