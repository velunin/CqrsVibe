using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using GreenPipes;
using GreenPipes.Configurators;
using GreenPipes.Filters;

namespace CqrsVibe
{
    internal static class CommonPipeConfiguratorExtensions
    {
        private static readonly ConcurrentDictionary<LocationKey, object> PipesCache =
            new ConcurrentDictionary<LocationKey, object>();
        
        public static void UseRouteFor<TConcreteContext, TOriginalContext>(
            this IPipeConfigurator<TOriginalContext> originalPipeConfigurator,
            Func<TOriginalContext,bool> filter,
            Action<IPipeConfigurator<TConcreteContext>> configure) 
            where TConcreteContext : class, TOriginalContext, PipeContext 
            where TOriginalContext : class, PipeContext
        {
            originalPipeConfigurator.UseInlineFilter((context, next) =>
            {
                if (filter(context))
                {
                    var locationKey = new LocationKey(
                        next,
                        typeof(TConcreteContext));
                    
                    if (!PipesCache.TryGetValue(locationKey, out var pipeEntry))
                    {
                        pipeEntry = CreateRoutePipe(configure, next);
                        PipesCache.TryAdd(locationKey, pipeEntry);
                    }

                    return ((IPipe<TConcreteContext>)pipeEntry).Send((TConcreteContext) context);
                }

                return next.Send(context);
            });
        }

        private static IPipe<TConcreteContext> CreateRoutePipe<TConcreteContext, TOriginalContext>(
            Action<IPipeConfigurator<TConcreteContext>> configure, 
            IPipe<TOriginalContext> next)
            where TConcreteContext : class, TOriginalContext, PipeContext 
            where TOriginalContext : class, PipeContext
        {
            var routeConfigurator = new PipeConfigurator<TConcreteContext>();
            configure(routeConfigurator);

            var connector = new TeeFilter<TConcreteContext>();
            connector.ConnectPipe(next);

            routeConfigurator.UseFilter(connector);

            return routeConfigurator.Build();
        }
        
        private readonly struct LocationKey : IEquatable<LocationKey>
        {
            public LocationKey(object nextFilter, Type contextType)
            {
                NextFilterReference = RuntimeHelpers.GetHashCode(nextFilter);
                ContextType = contextType;
            }

            private int NextFilterReference { get; }

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