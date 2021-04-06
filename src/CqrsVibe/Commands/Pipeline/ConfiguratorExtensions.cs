using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;

namespace CqrsVibe.Commands.Pipeline
{
    public static class ConfiguratorExtensions
    {
        public static void UseForCommand<TCommand>(
            this IPipeConfigurator<ICommandContext> configurator, 
            Action<IPipeConfigurator<ICommandContext<TCommand>>> configure) 
            where TCommand : ICommand
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseDispatch(
                new ConcreteCommandContextConverterFactory(),
                cfg => cfg.Pipe(configure));
        }
        
        public static void UseForCommands(
            this IPipeConfigurator<ICommandContext> configurator, 
            Func<ICommand, bool> predicate, 
            Action<IPipeConfigurator<ICommandContext>> configure) 
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
                new CommandContextConverterFactory(predicate),
                cfg => cfg.Pipe(configure));
        }
        
        public static void UseForCommands(
            this IPipeConfigurator<ICommandContext> configurator, 
            HashSet<Type> commandTypes, 
            Action<IPipeConfigurator<ICommandContext>> configure) 
        {
            if (commandTypes == null)
            {
                throw new ArgumentNullException(nameof(commandTypes));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseDispatch(
                new CommandContextConverterFactory(command => commandTypes.Any(t => t == command.GetType())),
                cfg => cfg.Pipe(configure));
        }
        
        public static void UseForCommands(
            this IPipeConfigurator<ICommandContext> configurator, 
            IEnumerable<Type> commandTypes, 
            Action<IPipeConfigurator<ICommandContext>> configure) 
        {
            UseForCommands(configurator, commandTypes.ToHashSet(), configure);
        }
    }
}