using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.ContextAbstractions;
using GreenPipes;

namespace CqrsVibe.Commands
{
    public class CommandProcessor : ICommandProcessor
    {
        private readonly IPipe<ICommandHandlingContext> _commandPipe;

        private readonly ConcurrentDictionary<Type, Type> _commandHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        public CommandProcessor(
            IDependencyResolverAccessor resolverAccessor, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configurePipeline = null)
        {
            if (resolverAccessor == null)
            {
                throw new ArgumentNullException(nameof(resolverAccessor));
            }

            _commandPipe = Pipe.New<ICommandHandlingContext>(pipeConfigurator =>
            {
                pipeConfigurator.AddPipeSpecification(
                    new SetDependencyResolverSpecification<ICommandHandlingContext>(resolverAccessor));

                configurePipeline?.Invoke(pipeConfigurator);

                pipeConfigurator.AddPipeSpecification(new HandleCommandSpecification(resolverAccessor));
            });
        }
        
        public Task ProcessAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            
            var commandType = command.GetType();
            
            if (!_commandHandlerTypesCache.TryGetValue(commandType, out var commandHandlerType))
            {
                commandHandlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
                _commandHandlerTypesCache.TryAdd(commandType, commandHandlerType);
            }
            
            var context = CommandContextFactory.Create(command, commandHandlerType, null, cancellationToken);

            return _commandPipe.Send(context);
        }
        
        public async Task<TResult> ProcessAsync<TResult>(ICommand<TResult> command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var commandType = command.GetType();
            
            if (!_commandHandlerTypesCache.TryGetValue(commandType, out var commandHandlerType))
            {
                commandHandlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
                _commandHandlerTypesCache.TryAdd(commandType, commandHandlerType);
            }

            var context = CommandContextFactory.Create(command, commandHandlerType, typeof(TResult), cancellationToken);

            await _commandPipe.Send(context);
            var resultingContext = (IResultingHandlingContext) context;
            
            return ((Task<TResult>) resultingContext.ResultTask).Result;
        }

        internal static class CommandContextFactory
        {
            private static readonly ConcurrentDictionary<Type, Func<ICommand, Type, CancellationToken, ICommandHandlingContext>>
                ContextConstructorInvokers =
                    new ConcurrentDictionary<Type, Func<ICommand, Type, CancellationToken, ICommandHandlingContext>>();

            public static ICommandHandlingContext Create(
                ICommand command, 
                Type handlerType,
                Type resultType,
                CancellationToken cancellationToken)
            {
                var commandType = command.GetType();

                if (!ContextConstructorInvokers.TryGetValue(commandType, out var contextConstructorInvoker))
                {
                    contextConstructorInvoker = CreateContextConstructorInvoker(commandType,resultType);
                    ContextConstructorInvokers.TryAdd(commandType, contextConstructorInvoker);
                }

                return contextConstructorInvoker(command, handlerType, cancellationToken);
            }

            private static Func<ICommand,Type,CancellationToken,ICommandHandlingContext> CreateContextConstructorInvoker(Type commandType, Type resultType=null)
            {
                var contextType = resultType == null
                    ? typeof(CommandHandlingContext<>).MakeGenericType(commandType)
                    : typeof(CommandHandlingContext<,>).MakeGenericType(commandType, resultType);
                var contextConstructorInfo = contextType.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] {commandType, typeof(Type), typeof(CancellationToken)},
                    null);

                var commandLambdaParameter = Expression.Parameter(typeof(ICommand), "command");
                var handlerInterfaceParameter = Expression.Parameter(typeof(Type), "handlerInterface");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

                var concreteCommandInstance = Expression.Variable(commandType, "concreteCommand");

                var block = Expression.Block(new[] {concreteCommandInstance},
                    Expression.Assign(concreteCommandInstance, Expression.Convert(commandLambdaParameter, commandType)),
                    Expression.New(contextConstructorInfo!, concreteCommandInstance, handlerInterfaceParameter, cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<ICommand, Type, CancellationToken, CommandHandlingContext>>(
                        block, commandLambdaParameter, handlerInterfaceParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}