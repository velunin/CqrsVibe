using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.ContextAbstractions;
using CqrsVibe.Pipeline;
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
                pipeConfigurator.UseDependencyResolver(resolverAccessor);

                configurePipeline?.Invoke(pipeConfigurator);

                pipeConfigurator.UseHandleCommand(resolverAccessor);
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

            var contextConstructor = CommandContextCtorFactory.GetOrCreate(commandType, null);
            var context = contextConstructor.Construct(command, commandHandlerType, cancellationToken);

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

            var contextConstructor = CommandContextCtorFactory.GetOrCreate(commandType, typeof(TResult));
            var context = contextConstructor.Construct(command, commandHandlerType, cancellationToken);

            await _commandPipe.Send(context);
            var resultingContext = (IResultingHandlingContext) context;
            
            return ((Task<TResult>) resultingContext.ResultTask).Result;
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("commandProcessor");

            _commandPipe.Probe(scope.CreateScope("commandPipe"));
        }

        internal static class CommandContextCtorFactory
        {
            private static readonly ConcurrentDictionary<Type, CommandContextConstructor>
                ContextConstructorsCache =
                    new ConcurrentDictionary<Type, CommandContextConstructor>();

            public static CommandContextConstructor GetOrCreate(Type commandType, Type resultType)
            {
                if (!ContextConstructorsCache.TryGetValue(commandType, out var contextConstructor))
                {
                    contextConstructor = CommandContextConstructor.Compile(commandType, resultType);
                    ContextConstructorsCache.TryAdd(commandType, contextConstructor);
                }

                return contextConstructor;
            }
        }

        internal readonly struct CommandContextConstructor
        {
            private readonly Func<ICommand, Type, CancellationToken, ICommandHandlingContext> _ctorInvoker;

            private CommandContextConstructor(
                Type contextType,
                Func<ICommand, Type, CancellationToken, ICommandHandlingContext> ctorInvoker)
            {
                ContextType = contextType;
                _ctorInvoker = ctorInvoker;
            }

            public Type ContextType { get; }

            public ICommandHandlingContext Construct(
                ICommand command, 
                Type handlerType,
                CancellationToken cancellationToken)
            {
                return _ctorInvoker(command, handlerType, cancellationToken);
            }

            public static CommandContextConstructor Compile(Type commandType, Type resultType)
            {
                var contextType = resultType == null
                    ? typeof(CommandHandlingContext<>).MakeGenericType(commandType)
                    : typeof(CommandHandlingContext<,>).MakeGenericType(commandType, resultType);

                return new CommandContextConstructor(contextType, CompileCtorInvoker(commandType, contextType));
            }

            private static Func<ICommand, Type, CancellationToken, ICommandHandlingContext> CompileCtorInvoker(
                Type commandType, 
                Type contextType)
            {
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
                    Expression.New(contextConstructorInfo!, concreteCommandInstance, handlerInterfaceParameter,
                        cancellationTokenParameter));

                var constructorInvoker =
                    Expression.Lambda<Func<ICommand, Type, CancellationToken, ICommandHandlingContext>>(
                        block, commandLambdaParameter, handlerInterfaceParameter, cancellationTokenParameter);

                return constructorInvoker.Compile();
            }
        }
    }
}