using System;
using System.Collections.Generic;
using System.Linq;
using CqrsVibe.Pipeline;
using GreenPipes;

namespace CqrsVibe.Commands.Pipeline
{
    /// <summary>
    /// Command configurator extensions
    /// </summary>
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Configure a pipeline for specific command type
        /// </summary>
        /// <param name="configurator">Commands pipeline configurator</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <typeparam name="TCommand">Command type</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null</exception>
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

        /// <summary>
        /// Configuring a pipeline for commands matched a predicate
        /// </summary>
        /// <param name="configurator">Commands pipeline configurator</param>
        /// <param name="predicate">Command match predicate</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> or <paramref name="predicate"/> is null</exception>
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

        /// <summary>
        /// Configuring a pipeline for command types 
        /// </summary>
        /// <param name="configurator">Commands pipeline configurator</param>
        /// <param name="commandTypes">Command types</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> or <paramref name="commandTypes"/> is null</exception>
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

        /// <summary>
        /// Configuring a pipeline for command types 
        /// </summary>
        /// <param name="configurator">Commands pipeline configurator</param>
        /// <param name="commandTypes">Command types</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <sparamref name="configure"/> or <paramref name="commandTypes"/> is null</exception>
        public static void UseForCommands(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            IEnumerable<Type> commandTypes, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configure) 
        {
            UseForCommands(configurator, commandTypes.ToHashSet(), configure);
        }
    }
}