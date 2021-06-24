using System;
using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe.Commands.Pipeline
{
    public interface ICommandHandlingContext : IHandlingContext, IResultingHandlingContext
    {
        ICommand Command { get; }
    }

    public interface ICommandHandlingContext<out TCommand> : ICommandHandlingContext where TCommand : ICommand
    {
        new TCommand Command { get; }
    }

    internal class CommandHandlingContext<TCommand> : CommandHandlingContext, ICommandHandlingContext<TCommand> where TCommand : ICommand
    {
        public CommandHandlingContext(
            TCommand command,
            Type commandHandlerInterface, 
            CancellationToken cancellationToken) : base(command, commandHandlerInterface, cancellationToken)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
        
        public new TCommand Command { get; }
        
    }
    
    public class CommandHandlingContext : BaseHandlingContext, ICommandHandlingContext
    {
        protected CommandHandlingContext(
            ICommand command, 
            Type commandHandlerInterface,
            CancellationToken cancellationToken) 
            : base(cancellationToken)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            CommandHandlerInterface = commandHandlerInterface ?? throw new ArgumentNullException(nameof(commandHandlerInterface));
        }

        public void SetResult(Task result)
        {
            Result = result;
        }

        public ICommand Command { get; }

        public Type CommandHandlerInterface { get; }

        public Task Result { get; private set; }
    }
}