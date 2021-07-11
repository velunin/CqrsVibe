using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class CommandProcessingTests : BaseTest
    {
        [Test]
        public async Task Should_process_command_without_result()
        {
            var processor = new CommandProcessor(ResolverAccessor);

            await processor.ProcessAsync(new SomeCommand());

            Assert.Pass();
        }

        [Test]
        public async Task Should_process_command_with_result()
        {
            const string expectedResult = "test";
            var processor = new CommandProcessor(ResolverAccessor);

            var result = await processor.ProcessAsync(new SomeCommandWithResult(expectedResult));

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_command()
        {
            var pipelineForSomeCommandExecuted = false;
            var pipelineForAnotherCommandExecuted = false;
            var processor = new CommandProcessor(ResolverAccessor, configurator =>
            {
                configurator.UseForCommand<SomeCommand>(cfg =>
                    cfg.UseExecute(_ => pipelineForSomeCommandExecuted = true));

                configurator.UseForCommand<AnotherCommand>(cfg =>
                    cfg.UseExecute(_ => pipelineForAnotherCommandExecuted = true));
            });

            await processor.ProcessAsync(new SomeCommand());

            Assert.IsTrue(pipelineForSomeCommandExecuted);
            Assert.IsFalse(pipelineForAnotherCommandExecuted);
        }

        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_command_types()
        {
            var pipelineForSomeCommandExecuted = false;
            var pipelineForAnotherCommandExecuted = false;
            var processor = new CommandProcessor(ResolverAccessor, cfg =>
            {
                cfg.UseForCommands(
                    new[] {typeof(SomeCommand)}.ToHashSet(),
                    cfg2 =>
                        cfg2.UseExecute(_ => pipelineForSomeCommandExecuted = true));

                cfg.UseForCommands(
                    new[] {typeof(AnotherCommand)},
                    cfg2 =>
                        cfg2.UseExecute(_ => pipelineForAnotherCommandExecuted = true));
            });

            await processor.ProcessAsync(new SomeCommand());

            Assert.IsTrue(pipelineForSomeCommandExecuted);
            Assert.IsFalse(pipelineForAnotherCommandExecuted);
        }

        [Test]
        public void Should_throw_correct_exception()
        {
            const string expectedResult = "test";
            var processor = new CommandProcessor(ResolverAccessor);

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return processor.ProcessAsync(new SomeBuggyCommand(expectedResult));
            });
            
            Assert.AreEqual(expectedResult, exception.Message);
        }

        [Test]
        public async Task Should_process_pipeline_for_specified_command_in_correct_order()
        {
            var traceMock = new Mock<ITraceService>(MockBehavior.Strict);
            var s = new MockSequence();
            var processor = new CommandProcessor(ResolverAccessor, cfg =>
            {
                cfg.UseForCommand<SomeCommand>(c =>
                {
                    c.UseInlineFilter(async (context, next) =>
                    {
                        traceMock.Object.BeforeHandle();
                        await next.Send(context);
                        traceMock.Object.AfterHandle();
                    });
                });
                cfg.UseInlineFilter((context, next) =>
                {
                    traceMock.Object.Handle();
                    return Task.CompletedTask;
                });
            });

            traceMock.InSequence(s).Setup(m => m.BeforeHandle());
            traceMock.InSequence(s).Setup(m => m.Handle());
            traceMock.InSequence(s).Setup(m => m.AfterHandle());

            await processor.ProcessAsync(new SomeCommand());

            traceMock.Verify(m=>m.BeforeHandle());
            traceMock.Verify(m=>m.Handle());
            traceMock.Verify(m=>m.AfterHandle());
        }

        [Test]
        public async Task Playground()
        {
            Services.AddSingleton<MyMiddleware>();
            Services.AddSingleton<SomeService>();

            var processor = new CommandProcessor(Get<IDependencyResolverAccessor>(), cfg =>
            {
                cfg.Use(typeof(MyMiddleware));
            });

            await processor.ProcessAsync(new SomeCommand());
        }
        private class SomeCommand : ICommand
        {
        }
    
        private class AnotherCommand : ICommand
        {
        }
    
        // ReSharper disable once UnusedType.Local
        private class SomeCommandHandler : ICommandHandler<SomeCommand>
        {
            public Task HandleAsync(
                ICommandHandlingContext<SomeCommand> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    
        private class SomeCommandWithResult : ICommand<string>
        {
            public SomeCommandWithResult(string someProperty)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }
    
        // ReSharper disable once UnusedType.Local
        private class SomeCommandWithResultHandler : ICommandHandler<SomeCommandWithResult, string>
        {
            public Task<string> HandleAsync(
                ICommandHandlingContext<SomeCommandWithResult> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(context.Command.SomeProperty);
            }
        }

        private class SomeBuggyCommand : ICommand
        {
            public SomeBuggyCommand(string exceptionText)
            {
                ExceptionText = exceptionText;
            }

            public string ExceptionText { get; }
        }

        // ReSharper disable once UnusedType.Local
        private class SomeBuggyCommandHandler : ICommandHandler<SomeBuggyCommand>
        {
            public Task HandleAsync(ICommandHandlingContext<SomeBuggyCommand> context, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException(context.Command.ExceptionText);
            }
        }
    }

    public interface ITraceService
    {
        void BeforeHandle();
        void Handle();
        void AfterHandle();
    }

    public class MyMiddleware
    {
        private readonly SomeService _service;

        public MyMiddleware(SomeService service)
        {
            _service = service;
        }

        public Task Invoke(
            ICommandHandlingContext context,
            IPipe<ICommandHandlingContext> next)
        {
            _service.Print();
            return next.Send(context);
        }
    }

    public class SomeService
    {
        public void Print()
        {
            Console.WriteLine("Hello from my middleware");
        }
    }
}