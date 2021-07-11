using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Events
{
    public interface IEventDispatcher : IProbeSite
    {
        /// <summary>
        /// Dispatch an event
        /// </summary>
        /// <param name="event">Event to handle</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEvent">Event type</typeparam>
        Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    }
}