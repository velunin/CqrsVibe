using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe.Events
{
    public interface IEventHandler<in TEvent>
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default(CancellationToken));
    }
}