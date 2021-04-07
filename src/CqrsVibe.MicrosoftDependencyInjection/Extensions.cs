using System;
using System.Reflection;
using CqrsVibe;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events;
using CqrsVibe.Queries;
using GreenPipes;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.MicrosoftDependencyInjection
{
    public static class Extensions
    {
        public static IServiceCollection AddCqrsVibe<TResolver>(
            this IServiceCollection services, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configureCommands = null) 
            where TResolver : class, IHandlerResolver
        {
            services.AddSingleton<ICommandProcessor>(provider =>
                new CommandProcessor(
                    provider.GetRequiredService<IHandlerResolver>(), 
                    configureCommands));
            services.AddSingleton<IQueryService, QueryService>();
            services.AddSingleton<IEventDispatcher, EventDispatcher>();
            services.AddSingleton<IHandlerResolver, TResolver>();
            
            return services;
        }

        public static IServiceCollection AddCqrsHandlers(
            this IServiceCollection serviceCollection,
            ServiceLifetime lifetime,
            params Assembly[] fromAssemblies)
        {
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