using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;

namespace CqrsVibe.Queries.Pipeline
{
    public static class ConfiguratorExtensions
    {
        public static void UseForQuery<TQuery>(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            Action<IPipeConfigurator<IQueryHandlingContext<TQuery>>> configure) 
            where TQuery : IQuery
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            
            configurator.UseRouteFor(
                context => context is IQueryHandlingContext<TQuery>, 
                configure);
        }
        
        public static void UseForQueries(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            Func<IQuery, bool> predicate, 
            Action<IPipeConfigurator<IQueryHandlingContext>> configure) 
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            
            configurator.UseRouteFor(
                context => predicate(context.Query), 
                configure);
        }
        
        public static void UseForQueries(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            HashSet<Type> queryTypes, 
            Action<IPipeConfigurator<IQueryHandlingContext>> configure) 
        {
            if (queryTypes == null)
            {
                throw new ArgumentNullException(nameof(queryTypes));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            
            configurator.UseRouteFor(
                context => queryTypes.Contains(context.Query.GetType()), 
                configure);
        }
        
        public static void UseForQueries(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            IEnumerable<Type> queryTypes, 
            Action<IPipeConfigurator<IQueryHandlingContext>> configure) 
        {
            UseForQueries(configurator, queryTypes.ToHashSet(), configure);
        }
    }
}