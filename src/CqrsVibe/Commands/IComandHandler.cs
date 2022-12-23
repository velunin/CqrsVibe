using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;

namespace CqrsVibe.Commands
{
    /// <summary>
    /// Base interface for command handlers without result
    /// </summary>
    /// <typeparam name="TCommand">Command to handle</typeparam>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Handle command
        /// </summary>
        /// <param name="context">Command context</param>
        /// <param name="cancellationToken"></param>
        Task HandleAsync(ICommandHandlingContext<TCommand> context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Base interface for command handlers with result
    /// </summary>
    /// <typeparam name="TCommand">Command to handle</typeparam>
    /// <typeparam name="TResult">Command result value</typeparam>
    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        /// <summary>
        /// Handle command
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TResult> HandleAsync(ICommandHandlingContext<TCommand> context,
            CancellationToken cancellationToken = default);
    }
}