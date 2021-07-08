using System;
using System.Linq.Expressions;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.ContextAbstractions;
using GreenPipes;

namespace CqrsVibe.Pipeline
{
    internal static class CommonPipeConfiguratorExtensions
    {
        public static void UseRouteFor<TRouteContext, TOriginalContext>(
            this IPipeConfigurator<TOriginalContext> originalPipeConfigurator,
            Expression<Func<TOriginalContext,bool>> filter,
            Action<IPipeConfigurator<TRouteContext>> configure) 
            where TRouteContext : class, TOriginalContext, PipeContext 
            where TOriginalContext : class, PipeContext
        {
            originalPipeConfigurator.AddPipeSpecification(
                new SpecificRouteFilterSpecification<TRouteContext,TOriginalContext>(filter, configure));
        }

        internal static void UseDependencyResolver<TContext>(
            this IPipeConfigurator<TContext> configurator, 
            IDependencyResolverAccessor resolverAccessor) 
            where TContext : class, IHandlingContext
        {
            configurator.AddPipeSpecification(new SetDependencyResolverSpecification<TContext>(resolverAccessor));
        }

        internal static void UseHandleCommand(
            this IPipeConfigurator<ICommandHandlingContext> configurator,
            IDependencyResolverAccessor resolverAccessor)
        {
            configurator.AddPipeSpecification(new HandleCommandSpecification(resolverAccessor));
        }
    }
}