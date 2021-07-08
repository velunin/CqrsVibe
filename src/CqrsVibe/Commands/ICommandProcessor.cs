using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Commands
{
    public interface ICommandProcessor : IProbeSite
    {
        Task ProcessAsync(ICommand command, CancellationToken cancellationToken = default);

        Task<TResult> ProcessAsync<TResult>(ICommand<TResult> command,
            CancellationToken cancellationToken = default);
    }
}