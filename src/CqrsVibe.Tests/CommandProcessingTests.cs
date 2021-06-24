using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using GreenPipes;
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
                    cfg.UseExecute(_ =>  pipelineForAnotherCommandExecuted = true));
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
}