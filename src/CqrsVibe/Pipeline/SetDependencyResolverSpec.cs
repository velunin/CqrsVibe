using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CqrsVibe.ContextAbstractions;
using GreenPipes;

namespace CqrsVibe.Pipeline
{
    internal class SetDependencyResolverSpec<TContext> : IPipeSpecification<TContext>
        where TContext : class, IHandlingContext
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public SetDependencyResolverSpec(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor;
        }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            builder.AddFilter(new SetResolverFilter<TContext>(_resolverAccessor));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    internal class SetResolverFilter<TContext> : IFilter<TContext> where TContext : class, IHandlingContext
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public SetResolverFilter(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor;
        }

        public Task Send(TContext context, IPipe<TContext> next)
        {
            if (context is BaseHandlingContext baseContext)
            {
                baseContext.ContextServices = _resolverAccessor.Current;
            }

            return next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("setResolver");
        }
    }
}