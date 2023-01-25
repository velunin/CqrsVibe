using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Configurators;
using GreenPipes.Filters;

namespace CqrsVibe.Pipeline
{
    internal class SpecificRouteFilterSpec<TRouteContext, TOriginalContext> :
        IPipeSpecification<TOriginalContext>
        where TOriginalContext : class, PipeContext
        where TRouteContext : class, TOriginalContext
    {
        private readonly Action<IPipeConfigurator<TRouteContext>> _configureRoutePipe;
        private readonly Expression<Func<TOriginalContext, bool>> _filter;

        public SpecificRouteFilterSpec(
            Expression<Func<TOriginalContext, bool>> filter,
            Action<IPipeConfigurator<TRouteContext>> configureRoutePipe)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _configureRoutePipe = configureRoutePipe ?? throw new ArgumentNullException(nameof(configureRoutePipe));
        }

        public void Apply(IPipeBuilder<TOriginalContext> builder)
        {
            builder.AddFilter(new SpecificRouteFilter<TRouteContext, TOriginalContext>(_filter, _configureRoutePipe));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    internal class SpecificRouteFilter<TRouteContext, TOriginalContext> : IFilter<TOriginalContext>
        where TOriginalContext : class, PipeContext
        where TRouteContext : class, TOriginalContext
    {
        private static readonly ConcurrentDictionary<LocationKey, object> PipesCache =
            new ConcurrentDictionary<LocationKey, object>();

        private readonly Action<IPipeConfigurator<TRouteContext>> _configureRoutePipe;
        private readonly Expression<Func<TOriginalContext, bool>> _filter;
        private readonly Func<TOriginalContext, bool> _compiledFilter;

        public SpecificRouteFilter(
            Expression<Func<TOriginalContext, bool>> filter,
            Action<IPipeConfigurator<TRouteContext>> configureRoutePipe)
        {
            _filter = filter;
            _compiledFilter = filter.Compile();
            _configureRoutePipe = configureRoutePipe;
        }

        public Task Send(TOriginalContext context, IPipe<TOriginalContext> next)
        {
            if (_compiledFilter(context))
            {
                var locationKey = new LocationKey(
                    next,
                    typeof(TRouteContext));

                var pipeEntry = PipesCache.GetOrAdd(locationKey,
                    (locationKeyArg, configureRoutePipe) =>
                        CreateRoutePipe(locationKeyArg.NextFilter, configureRoutePipe),
                    _configureRoutePipe);


                return ((IPipe<TRouteContext>)pipeEntry).Send((TRouteContext)context);
            }

            return next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("routeFor");
            scope.Add("predicate", _filter.Body);

            var fakePipe = Pipe.New<TRouteContext>(cfg => _configureRoutePipe(cfg));
            fakePipe.Probe(scope.CreateScope("pipeline"));
        }

        private static IPipe<TRouteContext> CreateRoutePipe(
            object next, Action<IPipeConfigurator<TRouteContext>> configureRoutePipe)
        {
            var routeConfigurator = new PipeConfigurator<TRouteContext>();
            configureRoutePipe(routeConfigurator);

            var connector = new TeeFilter<TRouteContext>();
            connector.ConnectPipe((IPipe<TRouteContext>)next);

            routeConfigurator.UseFilter(connector);

            return routeConfigurator.Build();
        }

        private readonly struct LocationKey : IEquatable<LocationKey>
        {
            public LocationKey(object nextFilter, Type contextType)
            {
                NextFilter = nextFilter;
                NextFilterReference = RuntimeHelpers.GetHashCode(nextFilter);
                ContextType = contextType;
            }

            private int NextFilterReference { get; }

            public object NextFilter { get; }

            private Type ContextType { get; }

            public bool Equals(LocationKey other)
            {
                return NextFilterReference == other.NextFilterReference && ContextType == other.ContextType;
            }

            public override bool Equals(object obj)
            {
                return obj is LocationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(NextFilterReference, ContextType);
            }
        }
    }
}