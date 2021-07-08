using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Events
{
    public interface IEventDispatcher : IProbeSite
    {
        Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    }
}