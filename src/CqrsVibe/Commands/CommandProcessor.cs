using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;
using GreenPipes;

namespace CqrsVibe.Commands
{
    public class CommandProcessor : ICommandProcessor
    {
        private readonly IPipe<ICommandHandlingContext> _commandPipe;

        private readonly ConcurrentDictionary<Type, Type> _commandHandlerTypesCache =
            new ConcurrentDictionary<Type, Type>();

        public CommandProcessor(
            IHandlerResolver handlerResolver, 
            Action<IPipeConfigurator<ICommandHandlingContext>> configurePipeline = null)
        {
            if (handlerResolver == null)
            {
                throw new ArgumentNullException(nameof(handlerResolver));
            }
            
            _commandPipe = Pipe.New<ICommandHandlingContext>(pipeConfigurator =>
            {
                configurePipeline?.Invoke(pipeConfigurator);
                
                pipeConfigurator.AddPipeSpecification(new HandleCommandSpecification(handlerResolver));
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
            
            var context = CommandContextFactory.Create(command, commandHandlerType, cancellationToken);

            return _commandPipe.Send(context);
        }
        
        public Task<TResult> ProcessAsync<TResult>(ICommand<TResult> command,
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
            
            var context = CommandContextFactory.Create(command, commandHandlerType, cancellationToken);

            return _commandPipe
                .Send(context)
                .ContinueWith(
                    sendTask =>
                    {
                        if (sendTask.IsFaulted && sendTask.Exception != null)
                        {
                            throw sendTask.Exception.GetBaseException();
                        }

                        return ((Task<TResult>) context.Result).Result;
                    }, cancellationToken);
        }

        internal static class CommandContextFactory
        {
            private static readonly ConcurrentDictionary<Type, Func<ICommand, Type, CancellationToken, CommandHandlingContext>>
                ContextConstructorInvokers =
                    new ConcurrentDictionary<Type, Func<ICommand, Type, CancellationToken, CommandHandlingContext>>();
                    
            public static CommandHandlingContext Create(
                ICommand command, 
                Type handlerType,
                CancellationToken cancellationToken)
            {
                var commandType = command.GetType();

                if (!ContextConstructorInvokers.TryGetValue(commandType, out var contextConstructorInvoker))
                {
                    contextConstructorInvoker = CreateContextConstructorInvoker(commandType);
                    ContextConstructorInvokers.TryAdd(commandType, contextConstructorInvoker);
                }

                return contextConstructorInvoker(command, handlerType, cancellationToken);
            }

            private static Func<ICommand,Type,CancellationToken,CommandHandlingContext> CreateContextConstructorInvoker(Type commandType)
            {
                var contextType = typeof(CommandHandlingContext<>).MakeGenericType(commandType);
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