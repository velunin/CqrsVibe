using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events;
using CqrsVibe.Events.Pipeline;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class EventProcessingTests : BaseTest
    {
        [Test]
        public async Task Should_process_event()
        {
            var dispatcher = new EventDispatcher(ResolverAccessor);

            await dispatcher.DispatchAsync(new SomeEvent());

            Assert.Pass();
        }

        private struct SomeEvent
        {
        }

        // ReSharper disable once UnusedType.Local
        private class SomeEventHandler : IEventHandler<SomeEvent>
        {
            public Task HandleAsync(IEventHandlingContext<SomeEvent> context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
        
        // ReSharper disable once UnusedType.Local
        private class SomeEventHandler2 : IEventHandler<SomeEvent>
        {
            public Task HandleAsync(IEventHandlingContext<SomeEvent> context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}