using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe.Events.Pipeline
{
    internal class HandleEventSpecification : IPipeSpecification<IEventHandlingContext>
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly HandlerInvokerFactory<IEventHandlingContext> _eventHandlerInvokerFactory;

        public HandleEventSpecification(IHandlerResolver handlerResolver)
        {
            _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
            _eventHandlerInvokerFactory = new HandlerInvokerFactory<IEventHandlingContext>();
        }

        public void Apply(IPipeBuilder<IEventHandlingContext> builder)
        {
            builder.AddFilter(new InlineFilter<IEventHandlingContext>(async (context, next) =>
            {
                var eventContext = (EventHandlingContext) context;  
      
                var eventHandlerInvoker = _eventHandlerInvokerFactory.GetOrCreate(
                    eventContext.GetType(), 
                    eventContext.EventHandlerInterface);

                var eventHandlers = _handlerResolver.ResolveHandlers(eventHandlerInvoker.HandlerInterface);

                var handleTasks = eventHandlers?
                                      .Select(handler =>
                                          eventHandlerInvoker.HandleAsync(handler, context, context.CancellationToken))
                                  ?? Enumerable.Empty<Task>();

                var tcs = new TaskCompletionSource<object>();
                context.CancellationToken.Register(
                    () => tcs.TrySetCanceled(), 
                    false);
                
                await await Task.WhenAny(
                    Task.WhenAll(handleTasks),
                    tcs.Task);
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}