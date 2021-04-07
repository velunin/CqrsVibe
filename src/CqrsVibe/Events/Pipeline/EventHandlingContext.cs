using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Events.Pipeline
{
    public interface IEventHandlingContext : PipeContext
    {
        object Event { get; }
    }

    public interface IEventHandlingContext<out TEvent> : IEventHandlingContext
    {
        new TEvent Event { get; }
    }

    internal class EventHandlingContext : BasePipeContext, IEventHandlingContext
    {
        protected EventHandlingContext(object @event, Type eventHandlerInterface, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
            EventHandlerInterface =
                eventHandlerInterface ?? throw new ArgumentNullException(nameof(eventHandlerInterface));
        }

        public object Event { get; }

        public Type EventHandlerInterface { get; }
    }
    
    internal class EventHandlingContext<TEvent> : EventHandlingContext, IEventHandlingContext<TEvent>
    {
        public EventHandlingContext(TEvent @event, Type eventHandlerInterface, CancellationToken cancellationToken) 
            : base(@event, eventHandlerInterface, cancellationToken)
        {
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
        }

        public new TEvent Event { get; }
    }
}