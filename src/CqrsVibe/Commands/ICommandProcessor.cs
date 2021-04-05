using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe.Commands
{
    public interface ICommandProcessor
    {
        Task ProcessAsync(ICommand command, CancellationToken cancellationToken = default);

        Task<TResult> ProcessAsync<TResult>(IResultingCommand<TResult> command,
            CancellationToken cancellationToken = default);
    }
}