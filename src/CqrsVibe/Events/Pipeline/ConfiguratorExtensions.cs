using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;

namespace CqrsVibe.Events.Pipeline
{
    public static class ConfiguratorExtensions
    {
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
        
        public static void UseForEvents(
            this IPipeConfigurator<IEventHandlingContext> configurator, 
            IEnumerable<Type> eventTypes, 
            Action<IPipeConfigurator<IEventHandlingContext>> configure) 
        {
            UseForEvents(configurator, eventTypes.ToHashSet(), configure);
        }
    }
}