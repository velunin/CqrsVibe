using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CqrsVibe.Commands;
using CqrsVibe.Events;
using CqrsVibe.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    /// <summary>
    /// IServiceCollection extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Registration of CqrsVibe services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Delegate for configure pipelines of handling</param>
        public static IServiceCollection AddCqrsVibe(
            this IServiceCollection services, 
            Action<HandlingOptions> configure = null)
        {
            var options = new HandlingOptions();
            configure?.Invoke(options);
           
            services.AddSingleton<ICommandProcessor>(provider =>
                new CommandProcessor(
                    provider.GetRequiredService<IDependencyResolverAccessor>(),
                    configurator => options.CommandsCfg?.Invoke(provider, configurator)));

            services.AddSingleton<IQueryService>(provider =>
                new QueryService(
                    provider.GetRequiredService<IDependencyResolverAccessor>(), 
                    configurator => options.QueriesCfg?.Invoke(provider, configurator)));

            services.AddSingleton<IEventDispatcher>(provider =>
                new EventDispatcher(
                    provider.GetRequiredService<IDependencyResolverAccessor>(), 
                    configurator => options.EventsCfg?.Invoke(provider, configurator)));

            services.AddSingleton<IDependencyResolver, DependencyResolver>();
            services.AddSingleton<IDependencyResolverAccessor, DependencyResolverAccessor>();

            return services;
        }

        /// <summary>
        /// Registration of CqrsVibe handlers
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="lifetime">Lifetime</param>
        /// <param name="fromAssemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        public static IServiceCollection AddCqrsVibeHandlers(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            Assembly[] fromAssemblies = null,
            bool warmUpHandlerInvokersCache = true)
        {
            if (fromAssemblies == null || !fromAssemblies.Any())
            {
                fromAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            services
                .AddCqrsVibeCommandHandlers(lifetime, fromAssemblies, warmUpHandlerInvokersCache)
                .AddCqrsVibeQueryHandlers(lifetime, fromAssemblies, warmUpHandlerInvokersCache)
                .AddCqrsVibeEventHandlers(lifetime, fromAssemblies, warmUpHandlerInvokersCache);

            return services;
        }

        /// <summary>
        /// Registration of CqrsVibe query handlers
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="lifetime">Lifetime</param>
        /// <param name="fromAssemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        public static IServiceCollection AddCqrsVibeQueryHandlers(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            IEnumerable<Assembly> fromAssemblies,
            bool warmUpHandlerInvokersCache = true)
        {
            foreach (var handlerTypeDescriptor in AssemblyScanner.FindQueryHandlersFrom(
                fromAssemblies,
                warmUpHandlerInvokersCache))
            {
                services.Add(new ServiceDescriptor(
                    handlerTypeDescriptor.HandlerType,
                    handlerTypeDescriptor.ImplementationType,
                    lifetime));
            }

            return services;
        }

        /// <summary>
        /// Registration of CqrsVibe command handlers
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="lifetime">Lifetime</param>
        /// <param name="fromAssemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        public static IServiceCollection AddCqrsVibeCommandHandlers(
            this IServiceCollection services, 
            ServiceLifetime lifetime,
            IEnumerable<Assembly> fromAssemblies,
            bool warmUpHandlerInvokersCache = true)
        {
            foreach (var handlerTypeDescriptor in AssemblyScanner.FindCommandHandlersFrom(
                fromAssemblies, 
                warmUpHandlerInvokersCache))
            {
                services.Add(new ServiceDescriptor(
                    handlerTypeDescriptor.HandlerType,
                    handlerTypeDescriptor.ImplementationType,
                    lifetime));
            }

            return services;
        }

        /// <summary>
        /// Registration of CqrsVibe event handlers
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="lifetime">Lifetime</param>
        /// <param name="fromAssemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        public static IServiceCollection AddCqrsVibeEventHandlers(
            this IServiceCollection services, 
            ServiceLifetime lifetime,
            IEnumerable<Assembly> fromAssemblies,
            bool warmUpHandlerInvokersCache = true)
        {
            foreach (var handlerTypeDescriptor in AssemblyScanner.FindEventHandlersFrom(
                fromAssemblies, 
                warmUpHandlerInvokersCache))
            {
                services.Add(new ServiceDescriptor(
                    handlerTypeDescriptor.HandlerType,
                    handlerTypeDescriptor.ImplementationType,
                    lifetime));
            }

            return services;
        }

        /// <summary>
        /// Set service provider as current resolver
        /// </summary>
        public static void SetAsCurrentResolver(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<IDependencyResolverAccessor>().Current = new DependencyResolver(serviceProvider);
        }
    }
}