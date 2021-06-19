using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events.Pipeline;
using GreenPipes;

namespace CqrsVibe.Events
{
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IPipe<IEventHandlingContext> _eventHandlePipe;

        private readonly ConcurrentDictionary<Type, Type> _eventHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        public EventDispatcher(IDependencyResolverAccessor resolverAccessor, Action<IPipeConfigurator<IEventHandlingContext>> configurePipeline = null)
        {
            if (resolverAccessor == null)
            {
                throw new ArgumentNullException(nameof(resolverAccessor));
            }
            
            _eventHandlePipe = Pipe.New<IEventHandlingContext>(pipeConfigurator =>
            {
                pipeConfigurator.AddPipeSpecification(
                    new SetDependencyResolverSpecification<IEventHandlingContext>(resolverAccessor));
                
                configurePipeline?.Invoke(pipeConfigurator);
                
                pipeConfigurator.AddPipeSpecification(new HandleEventSpecification(resolverAccessor));
            });
        }

        public Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var eventType = @event.GetType();
            
            if (!_eventHandlerTypesCache.TryGetValue(eventType, out var eventHandlerType))
            {
                eventHandlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
                _eventHandlerTypesCache.TryAdd(eventType, eventHandlerType);
            }

            var context = EventContextFactory.Create(@event, eventHandlerType, cancellationToken);

            return _eventHandlePipe.Send(context);
        }

        internal static class EventContextFactory
        {
            private static readonly ConcurrentDictionary<Type, Func<object, Type, CancellationToken, EventHandlingContext>>
                ContextConstructorInvokers =
                    new ConcurrentDictionary<Type, Func<object, Type, CancellationToken, EventHandlingContext>>();
                    
            public static EventHandlingContext Create(
                object @event, 
                Type handlerInterface,
                CancellationToken cancellationToken)
            {
                var eventType = @event.GetType();

                if (!ContextConstructorInvokers.TryGetValue(eventType, out var contextConstructorInvoker))
                {
                    contextConstructorInvoker = CreateContextConstructorInvoker(eventType);
                    ContextConstructorInvokers.TryAdd(eventType, contextConstructorInvoker);
                }

                return contextConstructorInvoker(@event, handlerInterface, cancellationToken);
            }

            private static Func<object,Type,CancellationToken,EventHandlingContext> CreateContextConstructorInvoker(Type eventType)
            {
                var contextType = typeof(EventHandlingContext<>).MakeGenericType(eventType);
                var contextConstructorInfo = contextType.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] {eventType, typeof(Type), typeof(CancellationToken)},
                    null);
                
                var eventParameter = Expression.Parameter(typeof(object), "@event");
                var handlerInterfaceParameter = Expression.Parameter(typeof(Type), "handlerInterface");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                var concreteEventInstance = Expression.Variable(eventType, "concreteEvent");

                var block = Expression.Block(new[] {concreteEventInstance},
                    Expression.Assign(concreteEventInstance, Expression.Convert(eventParameter, eventType)),
                    Expression.New(contextConstructorInfo!, concreteEventInstance, handlerInterfaceParameter, cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<object, Type, CancellationToken, EventHandlingContext>>(
                        block, eventParameter, handlerInterfaceParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}