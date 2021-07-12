using System;
using System.Threading;
using CqrsVibe.ContextAbstractions;

namespace CqrsVibe.Events.Pipeline
{
    /// <summary>
    /// Base interface of event handling context
    /// </summary>
    public interface IEventHandlingContext : IHandlingContext
    {
        /// <summary>
        /// Event to handle
        /// </summary>
        object Event { get; }
    }

    /// <summary>
    /// Base interface of event handling context
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface IEventHandlingContext<out TEvent> : IEventHandlingContext
    {
        /// <summary>
        /// Event to handle
        /// </summary>
        new TEvent Event { get; }
    }

    /// <summary>
    /// Base event handling context
    /// </summary>
    internal abstract class EventHandlingContext : BaseHandlingContext, IEventHandlingContext
    {
        protected EventHandlingContext(object @event, Type eventHandlerInterface, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            EventHandlerInterface =
                eventHandlerInterface ?? throw new ArgumentNullException(nameof(eventHandlerInterface));
        }

        /// <summary>
        /// Event to handle
        /// </summary>
        public object Event { get; }

        /// <summary>
        /// Type of event handler interface
        /// </summary>
        public Type EventHandlerInterface { get; }
    }

    /// <summary>
    /// Event handling context
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    internal class EventHandlingContext<TEvent> : EventHandlingContext, IEventHandlingContext<TEvent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlingContext"/> class.
        /// </summary>
        /// <param name="event">Event to handle</param>
        /// <param name="eventHandlerInterface">Type of event handler interface</param>
        /// <param name="cancellationToken"></param>
        public EventHandlingContext(TEvent @event, Type eventHandlerInterface, CancellationToken cancellationToken) 
            : base(@event, eventHandlerInterface, cancellationToken)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
        }

        /// <summary>
        /// Event to handle
        /// </summary>
        public new TEvent Event { get; }
    }
}