using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events;
using CqrsVibe.MicrosoftDependencyInjection;
using CqrsVibe.Pipeline;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using GreenPipes.Configurators;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class ReflectionTests : BaseTest
    {
        [Test]
        public void Should_create_specific_command_context()
        {
            var contextConstructor = CommandProcessor.CommandContextCtorFactory.GetOrCreate(
                typeof(SomeCommand), 
                resultType:null);
            var context = contextConstructor.Construct(
                new SomeCommand(), 
                typeof(ICommandHandler<>).MakeGenericType(typeof(SomeCommand)),
                CancellationToken.None);

            Assert.AreEqual(typeof(CommandHandlingContext<SomeCommand>), context.GetType());
        }

        [Test]
        public void Should_create_specific_query_context()
        {
            var contextConstructor =
                QueryService.QueryContextCtorFactory.GetOrCreate(typeof(SomeQuery), typeof(string));
            var context = contextConstructor.Construct(
                new SomeQuery(), 
                typeof(IQueryHandler<,>).MakeGenericType(typeof(SomeQuery), typeof(string)),
                CancellationToken.None);

            Assert.AreEqual(typeof(QueryHandlingContext<SomeQuery,string>), context.GetType());
        }

        [Test]
        public void Should_create_specific_event_context()
        {
            var contextConstructor = EventDispatcher.EventContextCtorFactory.GetOrCreate(typeof(SomeEvent));
            var context = contextConstructor.Construct(
                new SomeEvent(), 
                typeof(IEventHandler<>).MakeGenericType(typeof(SomeEvent)),
                CancellationToken.None);

            Assert.AreEqual(typeof(Events.Pipeline.EventHandlingContext<SomeEvent>), context.GetType());
        }

        [Test]
        public async Task Correct_middleware_should_be_called()
        {
            var middlewareExecuted = false;

            SetUpMiddleware();

            var scopeFactory = Get<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();

            scope.ServiceProvider.SetToHandlerResolverAccessor();

            var pipe = Pipe.New<ICommandHandlingContext>(cfg =>
            {
                cfg.UseDependencyResolver(Get<IDependencyResolverAccessor>());
                cfg.Use<ICorrectMiddleware>();
            });
            var context = CommandProcessor.CommandContextCtorFactory
                .GetOrCreate(
                    typeof(SomeCommand),
                    resultType: null)
                .Construct(
                    new SomeCommand(),
                    typeof(ICommandHandler<>).MakeGenericType(typeof(SomeCommand)),
                    default);

            await pipe.Send(context);

            Assert.IsTrue(middlewareExecuted);

            void SetUpMiddleware()
            {
                Services.AddScoped(typeof(ScopedService));

                var middlewareMock = new Mock<ICorrectMiddleware>();
                middlewareMock
                    .Setup(x => x.Invoke(
                        It.IsNotNull<ICommandHandlingContext>(),
                        It.IsNotNull<IPipe<ICommandHandlingContext>>(), 
                        It.IsNotNull<ScopedService>()))
                    .Callback(() => middlewareExecuted = true)
                    .Returns(Task.CompletedTask);

                Services.AddSingleton(middlewareMock.Object);
            }
        }

        [TestCase(typeof(IncorrectMiddlewares.WithoutInvokeMethod))]
        [TestCase(typeof(IncorrectMiddlewares.WithWrongInvokeReturnType))]
        [TestCase(typeof(IncorrectMiddlewares.WithIncorrectFirstArg))]
        [TestCase(typeof(IncorrectMiddlewares.WithIncorrectSecondArg))]
        public void Should_throw_exception_when_incorrect_middleware_passed(Type middlewareType)
        {
            var configurator = new PipeConfigurator<ICommandHandlingContext>();

            configurator.Use(middlewareType);

            Assert.Throws<InvalidOperationException>(() => configurator.Build());
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

        // ReSharper disable once MemberCanBePrivate.Global
        public interface ICorrectMiddleware
        {
            Task Invoke(
                ICommandHandlingContext context,
                IPipe<ICommandHandlingContext> next,
                ScopedService scopedService);
        }

        public class ScopedService
        {
        }

        public static class IncorrectMiddlewares
        {
            public class WithoutInvokeMethod
            {
            }

            public class WithWrongInvokeReturnType
            {
                public void Invoke(ICommandHandlingContext context, IPipe<ICommandHandlingContext> next)
                {
                }
            }

            public class WithIncorrectFirstArg
            {
                public Task Invoke(object context, IPipe<ICommandHandlingContext> next)
                {
                    return Task.CompletedTask;
                }
            }

            public class WithIncorrectSecondArg
            {
                public Task Invoke(ICommandHandlingContext context, object next)
                {
                    return Task.CompletedTask;
                }
            }
        }
    }
}