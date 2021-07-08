using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Events.Pipeline
{
    internal class HandleEventSpecification : IPipeSpecification<IEventHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleEventSpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor ?? throw new ArgumentNullException(nameof(resolverAccessor));
        }

        public void Apply(IPipeBuilder<IEventHandlingContext> builder)
        {
            builder.AddFilter(new HandleEventFilter(_resolverAccessor));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    internal class HandleEventFilter : IFilter<IEventHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleEventFilter(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor;
        }

        public async Task Send(IEventHandlingContext context, IPipe<IEventHandlingContext> next)
        {
            var eventContext = (EventHandlingContext) context;  
      
            var eventHandlerInvoker = HandlerInvokerFactory<IEventHandlingContext>.GetOrCreate(
                eventContext.GetType(), 
                eventContext.EventHandlerInterface);

            var eventHandlers = _resolverAccessor.Current.ResolveServices(eventHandlerInvoker.HandlerInterface);

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
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("handleEvent");
        }
    }
}