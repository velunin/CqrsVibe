using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.ContextAbstractions;

namespace CqrsVibe.Commands.Pipeline
{
    /// <summary>
    /// Base command context
    /// </summary>
    public interface ICommandHandlingContext : IHandlingContext
    {
        /// <summary>
        /// Command to handle
        /// </summary>
        ICommand Command { get; }

        /// <summary>
        /// Type of command handler interface
        /// </summary>
        public Type CommandHandlerInterface { get; }
    }

    /// <summary>
    /// Base command context
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ICommandHandlingContext<out TCommand> : ICommandHandlingContext where TCommand : ICommand
    {
        /// <summary>
        /// Command to handle
        /// </summary>
        new TCommand Command { get; }
    }

    // ReSharper disable once UnusedTypeParameter
    /// <summary>
    /// Base resulting command context
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public interface ICommandHandlingContext<out TCommand, TResult> : 
        ICommandHandlingContext<TCommand>, 
        IResultingHandlingContext
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// Base command handling context
    /// </summary>
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

        /// <summary>
        /// Command to handle
        /// </summary>
        public ICommand Command { get; }

        /// <summary>
        /// Type of command handler interface
        /// </summary>
        public Type CommandHandlerInterface { get; }
    }

    /// <summary>
    /// Command handling context for command without result
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    internal class CommandHandlingContext<TCommand> : CommandHandlingContext, 
        ICommandHandlingContext<TCommand> 
        where TCommand : ICommand
    {
        /// <param name="command">Command to handle</param>
        /// <param name="commandHandlerInterface">Type of command handler interface</param>
        /// <param name="cancellationToken"></param>
        public CommandHandlingContext(
            TCommand command,
            Type commandHandlerInterface, 
            CancellationToken cancellationToken) : base(command, commandHandlerInterface, cancellationToken)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        /// <summary>
        /// Command to handle
        /// </summary>
        public new TCommand Command { get; }
    }

    /// <summary>
    /// Command handling context for command with result
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    /// <typeparam name="TResult">Command result type</typeparam>
    internal class CommandHandlingContext<TCommand,TResult> : CommandHandlingContext<TCommand>, 
        ICommandHandlingContext<TCommand,TResult> 
        where TCommand : ICommand
    {
        private Task<TResult> _resultContainer;

        /// <param name="command">Command to handle</param>
        /// <param name="commandHandlerInterface">Type of command handler interface</param>
        /// <param name="cancellationToken"></param>
        public CommandHandlingContext(
            TCommand command,
            Type commandHandlerInterface,
            CancellationToken cancellationToken) : base(command, commandHandlerInterface, cancellationToken)
        {
        }

        /// <summary>
        /// Set command result value
        /// </summary>
        public void SetResult(object result)
        {
            _resultContainer = Task.FromResult((TResult) result);
        }

        /// <summary>
        /// Set the task of getting the command result
        /// </summary>
        public void SetResultTask(Task result)
        {
            _resultContainer = (Task<TResult>) result;
        }

        /// <summary>
        /// Extract command result as object
        /// </summary>
        public Task<object> ExtractResult()
        {
            return _resultContainer.ContinueWith(x => (object) x.Result);
        }

        /// <summary>
        /// The task of getting the command result
        /// </summary>
        public Task ResultTask => _resultContainer;
    }
}