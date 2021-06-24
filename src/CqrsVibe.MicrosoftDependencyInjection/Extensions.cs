using System;
using System.Linq;
using System.Reflection;
using CqrsVibe.Commands;
using CqrsVibe.Events;
using CqrsVibe.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    public static class Extensions
    {
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

        public static IServiceCollection AddCqrsVibeHandlers(
            this IServiceCollection serviceCollection,
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            Assembly[] fromAssemblies = null)
        {
            if (fromAssemblies == null || !fromAssemblies.Any())
            {
                fromAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            
            serviceCollection.Scan(scan =>
                scan.FromAssemblies(fromAssemblies)
                    .AddClasses(
                        classes => classes
                            .AssignableTo(typeof(IQueryHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithLifetime(lifetime));

            serviceCollection.Scan(scan =>
                scan.FromAssemblies(fromAssemblies)
                    .AddClasses(
                        classes => classes
                            .AssignableTo(typeof(ICommandHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithLifetime(lifetime));

            serviceCollection.Scan(scan =>
                scan.FromAssemblies(fromAssemblies)
                    .AddClasses(
                        classes => classes
                            .AssignableTo(typeof(ICommandHandler<>)))
                    .AsImplementedInterfaces()
                    .WithLifetime(lifetime));

            serviceCollection.Scan(scan =>
                scan.FromAssemblies(fromAssemblies)
                    .AddClasses(
                        classes => classes
                            .AssignableTo(typeof(IEventHandler<>)))
                    .AsImplementedInterfaces()
                    .WithLifetime(lifetime));
            
            return serviceCollection;
        }

        public static void SetToHandlerResolverAccessor(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<IDependencyResolverAccessor>().Current = new DependencyResolver(serviceProvider);
        }
    }
}