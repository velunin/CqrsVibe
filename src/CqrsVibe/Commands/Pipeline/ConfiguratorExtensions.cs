using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;

namespace CqrsVibe.Commands.Pipeline
{
    public static class ConfiguratorExtensions
    {
        public static void UseForCommand<TCommand>(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            Action<IPipeConfigurator<ICommandHandlingContext<TCommand>>> configure) 
            where TCommand : ICommand
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseRouteFor(
                context => context is ICommandHandlingContext<TCommand>, 
                configure);
        }

        public static void UseForCommands(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            Func<ICommand, bool> predicate, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configure) 
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
                context=> predicate(context.Command), 
                configure);
        }
        
        public static void UseForCommands(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            HashSet<Type> commandTypes, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configure) 
        {
            if (commandTypes == null)
            {
                throw new ArgumentNullException(nameof(commandTypes));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            
            configurator.UseRouteFor(
                context=> commandTypes.Contains(context.Command.GetType()), 
                configure);
        }
        
        public static void UseForCommands(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            IEnumerable<Type> commandTypes, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configure) 
        {
            UseForCommands(configurator, commandTypes.ToHashSet(), configure);
        }
    }
}