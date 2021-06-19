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
    public class CommandProcessingTests
    {
        private readonly IDependencyResolverAccessor _resolverAccessor =
            new DependencyResolverAccessor(null);

        [Test]
        public async Task Should_process_command_without_result()
        {
            _resolverAccessor.Current = new DependencyResolver(() => new SomeCommandHandler());
            
            var processor = new CommandProcessor(_resolverAccessor);
            
            await processor.ProcessAsync(new SomeCommand());

            Assert.Pass();
        }

        [Test]
        public async Task Should_process_command_with_result()
        {
            const string expectedResult = "test";
            _resolverAccessor.Current = new DependencyResolver(() => new SomeCommandWithResultHandler());
            
            var processor = new CommandProcessor(_resolverAccessor);

            var result = await processor.ProcessAsync(new SomeCommandWithResult(expectedResult));

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_command()
        {
            var pipelineForSomeCommandExecuted = false;
            var pipelineForAnotherCommandExecuted = false;
            
            _resolverAccessor.Current = new DependencyResolver(() => new SomeCommandHandler());
            
            var processor = new CommandProcessor(_resolverAccessor, configurator =>
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

            _resolverAccessor.Current = new DependencyResolver(() => new SomeCommandHandler());

            var processor = new CommandProcessor(_resolverAccessor, configurator =>
            {
                configurator.UseForCommands(
                    new[] {typeof(SomeCommand)}.ToHashSet(),
                    cfg =>
                        cfg.UseExecute(_ => pipelineForSomeCommandExecuted = true));

                configurator.UseForCommands(
                    new[] {typeof(AnotherCommand)},
                    cfg =>
                        cfg.UseExecute(_ => pipelineForAnotherCommandExecuted = true));
            });

            await processor.ProcessAsync(new SomeCommand());
            
            Assert.IsTrue(pipelineForSomeCommandExecuted);
            Assert.IsFalse(pipelineForAnotherCommandExecuted);
        }

        [Test]
        public void Should_throw_correct_exception()
        {
            const string expectedResult = "test";
            _resolverAccessor.Current = new DependencyResolver(() => new SomeBuggyCommandHandler());
            
            var processor = new CommandProcessor(_resolverAccessor);

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

        private class SomeBuggyCommandHandler : ICommandHandler<SomeBuggyCommand>
        {
            public Task HandleAsync(ICommandHandlingContext<SomeBuggyCommand> context, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException(context.Command.ExceptionText);
            }
        }
    }
}