using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events;
using CqrsVibe.Events.Pipeline;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class EventProcessingTests
    {
        private readonly IDependencyResolverAccessor _resolverAccessor =
            new DependencyResolverAccessor(null);

        [Test]
        public async Task Should_process_event()
        {
            _resolverAccessor.Current = new DependencyResolver(multipleHandlerFactory: () =>
            {
                return new object[]
                {
                    new SomeEventHandler(),
                    new SomeEventHandler2()
                };
            });

            var dispatcher = new EventDispatcher(_resolverAccessor);

            await dispatcher.DispatchAsync(new SomeEvent());

            Assert.Pass();
        }

        private class SomeEvent
        {
        }

        private class SomeEventHandler : IEventHandler<SomeEvent>
        {
            public Task HandleAsync(IEventHandlingContext<SomeEvent> context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
        
        private class SomeEventHandler2 : IEventHandler<SomeEvent>
        {
            public Task HandleAsync(IEventHandlingContext<SomeEvent> context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}