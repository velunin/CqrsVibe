using System;
using System.Collections.Generic;
using System.Linq;
using CqrsVibe.Pipeline;
using GreenPipes;

namespace CqrsVibe.Queries.Pipeline
{
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Configure a pipeline for specific query type
        /// </summary>
        /// <param name="configurator">Queries pipeline configurator</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <typeparam name="TQuery">Query type</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> is null</exception>
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

        /// <summary>
        /// Configuring a pipeline for queries matched a predicate
        /// </summary>
        /// <param name="configurator">Queries pipeline configurator</param>
        /// <param name="predicate">Query match predicate</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> or <see cref="predicate"/> is null</exception>
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

        /// <summary>
        /// Configuring a pipeline for query types 
        /// </summary>
        /// <param name="configurator">Queries pipeline configurator</param>
        /// <param name="queryTypes">Query types</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> or <see cref="queryTypes"/> is null</exception>
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

        /// <summary>
        /// Configuring a pipeline for query types 
        /// </summary>
        /// <param name="configurator">Queries pipeline configurator</param>
        /// <param name="queryTypes">Query types</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> or <see cref="queryTypes"/> is null</exception>
        public static void UseForQueries(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            IEnumerable<Type> queryTypes, 
            Action<IPipeConfigurator<IQueryHandlingContext>> configure) 
        {
            UseForQueries(configurator, queryTypes.ToHashSet(), configure);
        }
    }
}