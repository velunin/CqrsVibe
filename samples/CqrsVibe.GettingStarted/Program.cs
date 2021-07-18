using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.MicrosoftDependencyInjection;
using CqrsVibe.Queries;
using GettingStartedApp.Commands;
using GettingStartedApp.Queries;
using GreenPipes;
using Microsoft.Extensions.DependencyInjection;

namespace GettingStartedApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var serviceProvider = ConfigureServiceProvider();

            var commandProcessor = serviceProvider.GetRequiredService<ICommandProcessor>();
            var queryService = serviceProvider.GetRequiredService<IQueryService>();
            
            Console.Write("Enter your name: ");
            var name = Console.ReadLine();

            await commandProcessor.ProcessAsync(
                new LogInUser(name));

            var greeting = await queryService.QueryAsync(
                new GetGreetingForCurrentUser());

            Console.WriteLine(greeting);
            Console.ReadKey();
        }

        private static IServiceProvider ConfigureServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddCqrsVibe(options =>
            {
                options.CommandsCfg = (provider, cfg) =>
                {
                    //Using InlineFilter to decorate command handling
                    cfg.UseInlineFilter(async (context, next) =>
                    {
                        var sw = new Stopwatch();
                        sw.Start();

                        await next.Send(context);

                        sw.Stop();
                        Console.WriteLine(
                            $"----------> {context.Command.GetType().Name} command execution took {sw.Elapsed}");
                    });
                };
            });
            services.AddCqrsVibeHandlers(
                fromAssemblies:new []
                {
                    typeof(LogInUser).Assembly
                });

            return services.BuildServiceProvider();
        }
    }
}