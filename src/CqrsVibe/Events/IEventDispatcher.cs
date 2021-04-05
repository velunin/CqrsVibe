using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe.Events
{
    public interface IEventDispatcher
    {
        Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    }
}