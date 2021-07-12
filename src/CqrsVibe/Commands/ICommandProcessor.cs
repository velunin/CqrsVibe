using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Commands
{
    /// <summary>
    /// Command processor
    /// </summary>
    public interface ICommandProcessor : IProbeSite
    {
        /// <summary>
        /// Executes command without result
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="cancellationToken"></param>
        Task ProcessAsync(ICommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes command with result
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult">Command result type</typeparam>
        /// <returns>Command result</returns>
        Task<TResult> ProcessAsync<TResult>(ICommand<TResult> command,
            CancellationToken cancellationToken = default);
    }
}