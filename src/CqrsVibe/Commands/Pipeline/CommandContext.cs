using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Commands.Pipeline
{
    public interface ICommandContext : PipeContext
    {
        ICommand Command { get; }
    }

    public interface ICommandContext<out TCommand> : ICommandContext where TCommand : ICommand
    {
        new TCommand Command { get; }
    }

    internal class CommandContext<TCommand> : CommandContext, ICommandContext<TCommand> where TCommand : ICommand
    {
        public CommandContext(
            TCommand command,
            Type handlerType, 
            CancellationToken cancellationToken) : base(command, handlerType, cancellationToken)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
        
        public new TCommand Command { get; }
        
    }
    
    internal class CommandContext : BasePipeContext, ICommandContext
    {
        protected CommandContext(
            ICommand command, 
            Type commandHandlerType,
            CancellationToken cancellationToken) 
            : base(cancellationToken)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            CommandHandlerType = commandHandlerType ?? throw new ArgumentNullException(nameof(commandHandlerType));
        }

        public ICommand Command { get; }

        public Type CommandHandlerType { get; }

        public Task Result { get; set; }
    }
}