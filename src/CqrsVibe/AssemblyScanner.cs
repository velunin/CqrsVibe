using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events;
using CqrsVibe.Events.Pipeline;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;

namespace CqrsVibe
{
    /// <summary>
    /// Helper for handlers finding
    /// </summary>
    public static class AssemblyScanner
    {
        /// <summary>
        /// Find command handlers from <paramref name="assemblies"/>
        /// </summary>
        /// <param name="assemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        /// <returns>The sequence of found handlers descriptors</returns>
        public static IEnumerable<HandlerTypeDescriptor> FindCommandHandlersFrom(
            IEnumerable<Assembly> assemblies, 
            bool warmUpHandlerInvokersCache = true)
        {
            foreach (var assembly in assemblies)
            {
                var implementations = assembly
                    .GetTypes()
                    .Where(type => !type.IsAbstract)
                    .Select(type => (handlerImplementationType:type,
                        handlerTypes:type.GetInterfaces().Where(i =>
                            i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                                                i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))));

                foreach (var (handlerImplementationType,handlerTypes) in implementations)
                {
                    foreach (var handlerType in handlerTypes)
                    {
                        if (warmUpHandlerInvokersCache)
                        {
                            var (commandType, resultType) = ExtractCommandAndResultTypes(handlerType);
                            var ctxCtor = 
                                CommandProcessor.CommandContextCtorFactory.GetOrCreate(commandType, resultType);
                            HandlerInvokerFactory<ICommandHandlingContext>.GetOrCreate(ctxCtor.ContextType, handlerType);
                        }

                        yield return new HandlerTypeDescriptor(handlerType, handlerImplementationType);
                    }
                }
            }
        }

        /// <summary>
        /// Find query handlers from <paramref name="assemblies"/>
        /// </summary>
        /// <param name="assemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        /// <returns>The sequence of found handlers descriptors</returns>
        public static IEnumerable<HandlerTypeDescriptor> FindQueryHandlersFrom(
            IEnumerable<Assembly> assemblies, 
            bool warmUpHandlerInvokersCache = true)
        {
            foreach (var assembly in assemblies)
            {
                var implementations = assembly
                    .GetTypes()
                    .Where(type => !type.IsAbstract)
                    .Select(type => (handlerImplementationType:type,
                        handlerTypes:type.GetInterfaces().Where(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))));

                foreach (var (handlerImplementationType,handlerTypes) in implementations)
                {
                    foreach (var handlerType in handlerTypes)
                    {
                        if (warmUpHandlerInvokersCache)
                        {
                            var (queryType, resultType) = ExtractQueryAndResultType(handlerType);
                            var ctxCtor =
                                QueryService.QueryContextCtorFactory.GetOrCreate(queryType, resultType);
                            HandlerInvokerFactory<IQueryHandlingContext>.GetOrCreate(ctxCtor.ContextType, handlerType);
                        }

                        yield return new HandlerTypeDescriptor(handlerType, handlerImplementationType);
                    }
                }
            }
        }

        /// <summary>
        /// Find event handlers from <paramref name="assemblies"/>
        /// </summary>
        /// <param name="assemblies">Assemblies containing handlers</param>
        /// <param name="warmUpHandlerInvokersCache">Warm up invokers cache of found handlers</param>
        /// <returns>The sequence of found handlers descriptors</returns>
        public static IEnumerable<HandlerTypeDescriptor> FindEventHandlersFrom(
            IEnumerable<Assembly> assemblies, 
            bool warmUpHandlerInvokersCache = true)
        {
            foreach (var assembly in assemblies)
            {
                var implementations = assembly
                    .GetTypes()
                    .Where(type => !type.IsAbstract)
                    .Select(type => (handlerImplementationType:type,
                        handlerTypes:type.GetInterfaces().Where(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))));

                foreach (var (handlerImplementationType,handlerTypes) in implementations)
                {
                    foreach (var handlerType in handlerTypes)
                    {
                        if (warmUpHandlerInvokersCache)
                        {
                            var eventType = ExtractEventType(handlerType);
                            var ctxCtor =
                                EventDispatcher.EventContextCtorFactory.GetOrCreate(eventType);
                            HandlerInvokerFactory<IEventHandlingContext>.GetOrCreate(ctxCtor.ContextType, handlerType);
                        }

                        yield return new HandlerTypeDescriptor(handlerType, handlerImplementationType);
                    }
                }
            }
        }

        private static (Type, Type) ExtractCommandAndResultTypes(Type handlerType)
        {
            switch (handlerType)
            {
                case var _ when handlerType.IsGenericType &&
                                handlerType.GetGenericTypeDefinition() == typeof(ICommandHandler<>):
                    return (handlerType.GetGenericArguments().First(), null);
                case var _ when handlerType.IsGenericType &&
                                handlerType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>):
                    var args = handlerType.GetGenericArguments();
                    return (args[0], args[1]);
                default:
                    throw new InvalidOperationException($"Can't extract command and result types from {handlerType}");
            }
        }

        private static (Type, Type) ExtractQueryAndResultType(Type handlerType)
        {
            if (!handlerType.IsGenericType ||
                handlerType.GetGenericTypeDefinition() != typeof(IQueryHandler<,>))
            {
                throw new InvalidOperationException($"Can't extract query and result types from {handlerType}");
            }

            var args = handlerType.GetGenericArguments();
            return (args[0], args[1]);
        }

        private static Type ExtractEventType(Type handlerType)
        {
            if (!handlerType.IsGenericType ||
                handlerType.GetGenericTypeDefinition() != typeof(IEventHandler<>))
            {
                throw new InvalidOperationException($"Can't extract event type from {handlerType}");
            }

            return handlerType.GetGenericArguments().First();
        }

        /// <summary>
        /// Handler type descriptor
        /// </summary>
        public readonly struct HandlerTypeDescriptor
        {
            /// <summary>
            /// Initializes a instance of <see cref="HandlerTypeDescriptor"/>
            /// </summary>
            /// <param name="handlerType"></param>
            /// <param name="implementationType"></param>
            public HandlerTypeDescriptor(Type handlerType, Type implementationType)
            {
                HandlerType = handlerType;
                ImplementationType = implementationType;
            }

            /// <summary>
            /// Handler interface type
            /// </summary>
            public Type HandlerType { get; }

            /// <summary>
            /// Handler implementation type
            /// </summary>
            public Type ImplementationType { get; }
        }
    }
}