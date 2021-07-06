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

        public EventDispatcher(
            IDependencyResolverAccessor resolverAccessor, 
            Action<IPipeConfigurator<IEventHandlingContext>> configurePipeline = null)
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

            var contextConstructor = EventContextCtorFactory.GetOrCreate(eventType);
            var context = contextConstructor.Construct(@event, eventHandlerType, cancellationToken);

            return _eventHandlePipe.Send(context);
        }

        internal static class EventContextCtorFactory
        {
            private static readonly ConcurrentDictionary<Type, EventContextConstructor>
                ContextConstructorsCache =
                    new ConcurrentDictionary<Type, EventContextConstructor>();

            public static EventContextConstructor GetOrCreate(Type eventType)
            {
                if (!ContextConstructorsCache.TryGetValue(eventType, out var contextConstructor))
                {
                    contextConstructor = EventContextConstructor.Compile(eventType);
                    ContextConstructorsCache.TryAdd(eventType, contextConstructor);
                }

                return contextConstructor;
            }
        }

        internal readonly struct EventContextConstructor
        {
            private readonly Func<object, Type, CancellationToken, IEventHandlingContext> _ctorInvoker;

            private EventContextConstructor(
                Type contextType,
                Func<object, Type, CancellationToken, IEventHandlingContext> ctorInvoker)
            {
                ContextType = contextType;
                _ctorInvoker = ctorInvoker;
            }

            public Type ContextType { get; }

            public IEventHandlingContext Construct(
                object @event, 
                Type handlerType,
                CancellationToken cancellationToken)
            {
                return _ctorInvoker(@event, handlerType, cancellationToken);
            }

            public static EventContextConstructor Compile(Type eventType)
            {
                var contextType = typeof(EventHandlingContext<>).MakeGenericType(eventType);

                return new EventContextConstructor(contextType, CompileCtorInvoker(eventType, contextType));
            }

            private static Func<object, Type, CancellationToken, IEventHandlingContext> CompileCtorInvoker(
                Type eventType, 
                Type contextType)
            {
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
                    Expression.New(contextConstructorInfo!, concreteEventInstance, handlerInterfaceParameter,
                        cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<object, Type, CancellationToken, EventHandlingContext>>(
                        block, eventParameter, handlerInterfaceParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}