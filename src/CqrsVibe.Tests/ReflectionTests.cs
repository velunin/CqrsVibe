using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Events;
using CqrsVibe.FluentValidation;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class ReflectionTests
    {
        [Test]
        public void Should_create_specific_command_context()
        {
            var context = CommandProcessor.CommandContextFactory.Create(
                new SomeCommand(), 
                typeof(ICommandHandler<>).MakeGenericType(typeof(SomeCommand)),
                CancellationToken.None);

            Assert.AreEqual(typeof(Commands.Pipeline.CommandHandlingContext<SomeCommand>), context.GetType());
        }
        
        [Test]
        public void Should_create_specific_query_context()
        {
            var context = QueryService.QueryContextFactory.Create(
                new SomeQuery(), 
                typeof(IQueryHandler<,>).MakeGenericType(typeof(SomeQuery), typeof(string)),
                CancellationToken.None);

            Assert.AreEqual(typeof(QueryHandlingContext<SomeQuery>), context.GetType());
        }
        
        [Test]
        public void Should_create_specific_event_context()
        {
            var context = EventDispatcher.EventContextFactory.Create(
                new SomeEvent(), 
                typeof(IEventHandler<>).MakeGenericType(typeof(SomeEvent)),
                CancellationToken.None);

            Assert.AreEqual(typeof(Events.Pipeline.EventHandlingContext<SomeEvent>), context.GetType());
        }

        [Test]
        public void Should_create_task_with_given_either_validation_result()
        {
            var validationState = new ValidationState();

            var task = ValidationResultTaskFactory
                    .Create(validationState, typeof(Either<string, ValidationState>)) 
                as Task<Either<string, ValidationState>>;

            Assert.NotNull(task);
        }

        private class SomeQuery : IQuery<string>
        {
        }

        private class SomeCommand : ICommand
        {
        }

        private class SomeEvent
        {
        }
    }
}