using System;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.MicrosoftDependencyInjection;
using CqrsVibe.Pipeline;
using CqrsVibe.PipelineConfiguration.Pipeline;
using CqrsVibe.PipelineConfiguration.Services;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsVibe.PipelineConfiguration
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var rootProvider = ConfigureServiceProvider();

            var queryService = rootProvider.GetRequiredService<IQueryService>();
            using var serviceScope = rootProvider.CreateScope();

            //Set as current resolver. Uses to resolve scoped dependencies
            serviceScope.ServiceProvider.SetAsCurrentResolver();

            var result = await queryService.QueryAsync(new SomeQuery());
        }

        private static IServiceProvider ConfigureServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton<SomeQueriesMiddleware>();
            services.AddSingleton<ISingletonService, SingletonService>();
            services.AddScoped<IScopedService, ScopedService>();

            services.AddCqrsVibe(options =>
            {
                options.CommandsCfg = CommandsHandlingConfiguration;
                options.QueriesCfg = QueriesHandlingConfiguration;
            });

            services.AddCqrsVibeHandlers(
                fromAssemblies: new[]
                {
                    typeof(SomeQuery).Assembly
                });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Configures the command processing pipeline
        /// </summary>
        private static void CommandsHandlingConfiguration(
            IServiceProvider provider,
            IPipeConfigurator<ICommandHandlingContext> cfg)
        {
            //Using standard GreenPipes filters 
            cfg.UseInlineFilter(async (context, next) =>
            {
                //Pre-processing logic
                await next.Send(context);
                //Post-processing logic
            });
            cfg.UseRetry(retry =>
            {
                retry
                    .Interval(3, TimeSpan.FromSeconds(1))
                    .Handle<OptimisticLockException>();
            });
            cfg.UseFilter(new SomeCommandFilter(provider.GetService<ISingletonService>()));
        }

        /// <summary>
        /// Configures the query handling pipeline
        /// </summary>
        private static void QueriesHandlingConfiguration(
            IServiceProvider provider,
            IPipeConfigurator<IQueryHandlingContext> cfg)
        {
            //Using standard CqrsVibe filters

            //Using middleware feature. Resolves middleware instance and insert calling into pipeline
            cfg.Use<SomeQueriesMiddleware>();
            
            //Configures and insert pipeline for SomeQuery
            cfg.UseForQuery<SomeQuery>(configurator =>
            {
                //Configuring pipeline
            });
        }
    }
}