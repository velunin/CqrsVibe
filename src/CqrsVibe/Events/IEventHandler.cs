using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events.Pipeline;

namespace CqrsVibe.Events
{
    public interface IEventHandler<in TEvent>
    {
        Task HandleAsync(IEventHandlingContext<TEvent> context, CancellationToken cancellationToken = default);
    }
}