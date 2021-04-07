using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using GreenPipes.Filters;
using GreenPipes.Internals.Extensions;

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
            builder.AddFilter(new InlineFilter<IEventHandlingContext>((context, next) =>
            {
                var eventContext = (EventHandlingContext) context;  
      
                var eventHandlerInvoker = _eventHandlerInvokerFactory.GetOrCreate(
                    eventContext.GetType(), 
                    eventContext.EventHandlerInterface);

                var eventHandlers = _handlerResolver.ResolveHandlers(eventHandlerInvoker.HandlerInterface);

                var handleTasks = eventHandlers?.Select(x =>
                    eventHandlerInvoker.HandleAsync(x, context, context.CancellationToken)) ?? Enumerable.Empty<Task>();

                var tcs = new TaskCompletionSource<object>();
                context.CancellationToken.Register(
                    () => tcs.TrySetCanceled(), 
                    false);
                
                return Task.WhenAny(
                    Task.WhenAll(handleTasks),
                    tcs.Task);
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
    
    internal class ConcreteEventContextConverterFactory : IPipeContextConverterFactory<IEventHandlingContext>
    {
        public IPipeContextConverter<IEventHandlingContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            var queryType = typeof(TOutput).GetClosingArguments(typeof(IQueryHandlingContext<>)).Single();
            
            return (IPipeContextConverter<IEventHandlingContext, TOutput>)Activator
                .CreateInstance(typeof(EventContextConverter<>)
                    .MakeGenericType(queryType));
        }

        private class EventContextConverter<T> : 
            IPipeContextConverter<IQueryHandlingContext, IQueryHandlingContext<T>>
            where T : IQuery
        {
            public bool TryConvert(IQueryHandlingContext input, out IQueryHandlingContext<T> output)
            {
                output = input as IQueryHandlingContext<T>;
                return output != null;
            }
        }
    }

    internal class EventContextConverterFactory : IPipeContextConverterFactory<IEventHandlingContext>
    {
        private readonly Func<object, bool> _filter;

        public EventContextConverterFactory(Func<object, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public IPipeContextConverter<IEventHandlingContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            return (IPipeContextConverter<IEventHandlingContext, TOutput>)new EventContextConverter(_filter);
        }
        
        private class EventContextConverter : IPipeContextConverter<IEventHandlingContext, IEventHandlingContext>
        {
            private readonly Func<object, bool> _filter;

            public EventContextConverter(Func<object, bool> filter)
            {
                _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }

            public bool TryConvert(IEventHandlingContext input, out IEventHandlingContext output)
            {
                if (!_filter(input.Event))
                {
                    output = null;
                    return false;
                }
                output = input;
                return true;
            }
        }
    }
}