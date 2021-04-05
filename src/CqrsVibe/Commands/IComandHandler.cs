using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;

namespace CqrsVibe.Commands
{
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task HandleAsync(ICommandContext<TCommand> context, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResult> where TCommand : IResultingCommand<TResult>
    {
        Task<TResult> HandleAsync(ICommandContext<TCommand> context,
            CancellationToken cancellationToken = default);
    }
}