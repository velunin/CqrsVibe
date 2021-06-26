using System.Collections.Generic;
using System.Linq;
using CqrsVibe.ContextAbstractions;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe
{
    internal class SetDependencyResolverSpecification<TContext> : IPipeSpecification<TContext>
        where TContext : class, IHandlingContext

    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public SetDependencyResolverSpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor;
        }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            builder.AddFilter(
                new DelegateFilter<TContext>(context =>
                {
                    if (context is BaseHandlingContext baseContext)
                    {
                        baseContext.ContextServices = _resolverAccessor.Current;
                    }
                }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}