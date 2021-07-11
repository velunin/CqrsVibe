using System;
using System.Linq.Expressions;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.ContextAbstractions;
using CqrsVibe.Events.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.Pipeline
{
    public static class ConfiguratorExtensions
    {
        public static void UseRouteFor<TRouteContext, TOriginalContext>(
            this IPipeConfigurator<TOriginalContext> originalPipeConfigurator,
            Expression<Func<TOriginalContext,bool>> filter,
            Action<IPipeConfigurator<TRouteContext>> configure) 
            where TRouteContext : class, TOriginalContext, PipeContext 
            where TOriginalContext : class, PipeContext
        {
            originalPipeConfigurator.AddPipeSpecification(
                new SpecificRouteFilterSpec<TRouteContext,TOriginalContext>(filter, configure));
        }

        public static void Use<TContext>(
            this IPipeConfigurator<TContext> configurator,
            Type middlewareType)
            where TContext : class, IHandlingContext
        {
            configurator.AddPipeSpecification(new HandlingMiddlewareFilterSpec<TContext>(middlewareType));
        }

        public static void Use<TMiddleware>(
            this IPipeConfigurator<ICommandHandlingContext> configurator)
        {
            configurator.Use(typeof(TMiddleware));
        }

        public static void Use<TMiddleware>(
            this IPipeConfigurator<IQueryHandlingContext> configurator)
        {
            configurator.Use(typeof(TMiddleware));
        }

        public static void Use<TMiddleware>(
            this IPipeConfigurator<IEventHandlingContext> configurator)
        {
            configurator.Use(typeof(TMiddleware));
        }

        internal static void UseDependencyResolver<TContext>(
            this IPipeConfigurator<TContext> configurator, 
            IDependencyResolverAccessor resolverAccessor) 
            where TContext : class, IHandlingContext
        {
            configurator.AddPipeSpecification(new SetDependencyResolverSpec<TContext>(resolverAccessor));
        }

        internal static void UseHandleCommand(
            this IPipeConfigurator<ICommandHandlingContext> configurator,
            IDependencyResolverAccessor resolverAccessor)
        {
            configurator.AddPipeSpecification(new HandleCommandSpecification(resolverAccessor));
        }

        internal static void UseHandleQuery(
            this IPipeConfigurator<IQueryHandlingContext> configurator,
            IDependencyResolverAccessor resolverAccessor)
        {
            configurator.AddPipeSpecification(new HandleQuerySpecification(resolverAccessor));
        }

        internal static void UseHandleEvent(
            this IPipeConfigurator<IEventHandlingContext> configurator,
            IDependencyResolverAccessor resolverAccessor)
        {
            configurator.AddPipeSpecification(new HandleEventSpecification(resolverAccessor));
        }
    }
}