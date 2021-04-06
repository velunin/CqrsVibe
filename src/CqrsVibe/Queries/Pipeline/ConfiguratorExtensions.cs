using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;

namespace CqrsVibe.Queries.Pipeline
{
    public static class ConfiguratorExtensions
    {
        public static void UseForQuery<TQuery>(
            this IPipeConfigurator<IQueryContext> configurator, 
            Action<IPipeConfigurator<IQueryContext<TQuery>>> configure) 
            where TQuery : IQuery
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseDispatch(
                new ConcreteQueryContextConverterFactory(),
                cfg => cfg.Pipe(configure));
        }
        
        public static void UseForQueries(
            this IPipeConfigurator<IQueryContext> configurator, 
            Func<IQuery, bool> predicate, 
            Action<IPipeConfigurator<IQueryContext>> configure) 
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseDispatch(
                new QueryContextConverterFactory(predicate),
                cfg => cfg.Pipe(configure));
        }
        
        public static void UseForQueries(
            this IPipeConfigurator<IQueryContext> configurator, 
            HashSet<Type> queryTypes, 
            Action<IPipeConfigurator<IQueryContext>> configure) 
        {
            if (queryTypes == null)
            {
                throw new ArgumentNullException(nameof(queryTypes));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseDispatch(
                new QueryContextConverterFactory(query => queryTypes.Any(t => t == query.GetType())),
                cfg => cfg.Pipe(configure));
        }
        
        public static void UseForQueries(
            this IPipeConfigurator<IQueryContext> configurator, 
            IEnumerable<Type> queryTypes, 
            Action<IPipeConfigurator<IQueryContext>> configure) 
        {
            UseForQueries(configurator, queryTypes.ToHashSet(), configure);
        }
    }
}