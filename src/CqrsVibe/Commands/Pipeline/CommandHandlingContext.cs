using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.ContextAbstractions;

namespace CqrsVibe.Commands.Pipeline
{
    public interface ICommandHandlingContext : IHandlingContext
    {
        ICommand Command { get; }

        public Type CommandHandlerInterface { get; }
    }

    public interface ICommandHandlingContext<out TCommand> : ICommandHandlingContext where TCommand : ICommand
    {
        new TCommand Command { get; }
    }

    public interface ICommandHandlingContext<out TCommand, TResult> : 
        ICommandHandlingContext<TCommand>, 
        IResultingHandlingContext
        where TCommand : ICommand
    {
    }

    internal abstract class CommandHandlingContext : BaseHandlingContext, ICommandHandlingContext
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

        public ICommand Command { get; }

        public Type CommandHandlerInterface { get; }
    }

    internal class CommandHandlingContext<TCommand> : CommandHandlingContext, 
        ICommandHandlingContext<TCommand> 
        where TCommand : ICommand
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
    
    internal class CommandHandlingContext<TCommand,TResult> : CommandHandlingContext<TCommand>, 
        ICommandHandlingContext<TCommand,TResult> 
        where TCommand : ICommand
    {
        private Task<TResult> _resultContainer;

        public CommandHandlingContext(
            TCommand command,
            Type commandHandlerInterface,
            CancellationToken cancellationToken) : base(command, commandHandlerInterface, cancellationToken)
        {
        }

        public void SetResult(object result)
        {
            _resultContainer = Task.FromResult((TResult) result);
        }

        public void SetResultTask(Task result)
        {
            _resultContainer = (Task<TResult>) result;
        }

        public Task<object> ExtractResult()
        {
            return _resultContainer.ContinueWith(x => (object) x.Result);
        }

        public  Task ResultTask => _resultContainer;
    }
}