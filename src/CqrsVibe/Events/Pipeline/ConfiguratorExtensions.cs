using System;
using System.Collections.Generic;
using System.Linq;
using CqrsVibe.Pipeline;
using GreenPipes;

namespace CqrsVibe.Events.Pipeline
{
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Configure a pipeline for specific event type
        /// </summary>
        /// <param name="configurator">Events pipeline configurator</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> is null</exception>
        public static void UseForEvent<TEvent>(
            this IPipeConfigurator<IEventHandlingContext> configurator, 
            Action<IPipeConfigurator<IEventHandlingContext<TEvent>>> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseRouteFor(
                context => context is IEventHandlingContext<TEvent>, 
                configure);
        }

        /// <summary>
        /// Configuring a pipeline for events matched a predicate
        /// </summary>
        /// <param name="configurator">Events pipeline configurator</param>
        /// <param name="predicate">Event match predicate</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> or <see cref="predicate"/> is null</exception>
        public static void UseForEvents(
            this IPipeConfigurator<IEventHandlingContext> configurator,
            Func<object, bool> predicate,
            Action<IPipeConfigurator<IEventHandlingContext>> configure)
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
                context => predicate(context.Event),
                configure);
        }

        /// <summary>
        /// Configuring a pipeline for event types 
        /// </summary>
        /// <param name="configurator">Events pipeline configurator</param>
        /// <param name="eventTypes">Event types</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> or <see cref="eventTypes"/> is null</exception>
        public static void UseForEvents(
            this IPipeConfigurator<IEventHandlingContext> configurator, 
            HashSet<Type> eventTypes, 
            Action<IPipeConfigurator<IEventHandlingContext>> configure) 
        {
            if (eventTypes == null)
            {
                throw new ArgumentNullException(nameof(eventTypes));
            }
            
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configurator.UseRouteFor(
                context => eventTypes.Contains(context.Event.GetType()), 
                configure);
        }

        /// <summary>
        /// Configuring a pipeline for event types 
        /// </summary>
        /// <param name="configurator">Events pipeline configurator</param>
        /// <param name="eventTypes">Event types</param>
        /// <param name="configure">Delegate for configure pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="configure"/> or <see cref="eventTypes"/> is null</exception>
        public static void UseForEvents(
            this IPipeConfigurator<IEventHandlingContext> configurator, 
            IEnumerable<Type> eventTypes, 
            Action<IPipeConfigurator<IEventHandlingContext>> configure) 
        {
            UseForEvents(configurator, eventTypes.ToHashSet(), configure);
        }
    }
}