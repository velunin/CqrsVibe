using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events.Pipeline;

namespace CqrsVibe.Events
{
    /// <summary>
    /// Base interface for all event handlers
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface IEventHandler<in TEvent>
    {
        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="context">Event handling context</param>
        /// <param name="cancellationToken"></param>
        Task HandleAsync(IEventHandlingContext<TEvent> context, CancellationToken cancellationToken = default);
    }
}