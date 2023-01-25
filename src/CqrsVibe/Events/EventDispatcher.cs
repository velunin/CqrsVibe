using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events.Pipeline;
using CqrsVibe.Pipeline;
using GreenPipes;

namespace CqrsVibe.Events
{
    /// <summary>
    /// Implementation of <see cref="IEventDispatcher"/> interface
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IPipe<IEventHandlingContext> _eventHandlePipe;

        private readonly ConcurrentDictionary<Type, Type> _eventHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDispatcher"/> class.
        /// </summary>
        /// <param name="resolverAccessor">Dependency resolver accessor</param>
        /// <param name="configurePipeline">Delegate for configure event handling pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resolverAccessor"/> is null</exception>
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
                pipeConfigurator.UseDependencyResolver(resolverAccessor);

                configurePipeline?.Invoke(pipeConfigurator);

                pipeConfigurator.UseHandleEvent(resolverAccessor);
            });
        }

        /// <summary>
        /// Dispatch an event
        /// </summary>
        /// <param name="event">Event to handle</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEvent">Event type</typeparam>
        public Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var eventType = @event.GetType();

            var eventHandlerType = _eventHandlerTypesCache.GetOrAdd(
                eventType,
                eventTypeArg => typeof(IEventHandler<>).MakeGenericType(eventTypeArg));

            var contextConstructor = EventContextCtorFactory.GetOrCreate(eventType);
            var context = contextConstructor.Construct(@event, eventHandlerType, cancellationToken);

            return _eventHandlePipe.Send(context);
        }

        /// <inheritdoc />
        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("eventDispatcher");

            _eventHandlePipe.Probe(scope.CreateScope("eventHandlePipe"));
        }

        internal static class EventContextCtorFactory
        {
            private static readonly ConcurrentDictionary<Type, EventContextConstructor>
                ContextConstructorsCache =
                    new ConcurrentDictionary<Type, EventContextConstructor>();

            public static EventContextConstructor GetOrCreate(Type eventType)
            {
                return ContextConstructorsCache.GetOrAdd(
                    eventType,
                    // ReSharper disable once ConvertClosureToMethodGroup
                    eventTypeArg => EventContextConstructor.Compile(eventTypeArg));
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
                    new[] { eventType, typeof(Type), typeof(CancellationToken) },
                    null);

                var eventParameter = Expression.Parameter(typeof(object), "@event");
                var handlerInterfaceParameter = Expression.Parameter(typeof(Type), "handlerInterface");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                var concreteEventInstance = Expression.Variable(eventType, "concreteEvent");

                var block = Expression.Block(new[] { concreteEventInstance },
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