using System;
using System.Linq;
using System.Reflection;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events;
using CqrsVibe.Events.Pipeline;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    public static class Extensions
    {
        public static IServiceCollection AddCqrsVibe<TResolver>(
            this IServiceCollection services, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configureCommands = null,
            Action<IPipeConfigurator<IQueryHandlingContext>> configureQueries = null,
            Action<IPipeConfigurator<IEventHandlingContext>> configureEvents = null) 
            where TResolver : class, IHandlerResolver
        {
            services.AddSingleton<ICommandProcessor>(provider =>
                new CommandProcessor(
                    provider.GetRequiredService<IHandlerResolver>(), 
                    configureCommands));
            
            services.AddSingleton<IQueryService>(provider =>
                new QueryService(
                    provider.GetRequiredService<IHandlerResolver>(), 
                    configureQueries));
            
            services.AddSingleton<IEventDispatcher>(provider =>
                new EventDispatcher(
                    provider.GetRequiredService<IHandlerResolver>(), 
                    configureEvents));
            
            services.AddSingleton<IHandlerResolver, TResolver>();
            
            return services;
        }

        public static IServiceCollection AddCqrsHandlers(
            this IServiceCollection serviceCollection,
            ServiceLifetime lifetime,
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
    }
}