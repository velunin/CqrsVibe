using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;

namespace CqrsVibe.Commands
{
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task HandleAsync(ICommandHandlingContext<TCommand> context, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(ICommandHandlingContext<TCommand> context,
            CancellationToken cancellationToken = default);
    }
}