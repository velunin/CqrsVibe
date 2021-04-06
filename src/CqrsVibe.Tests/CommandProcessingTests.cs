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
        [Test]
        public async Task Should_process_command_without_result()
        {
            var processor = new CommandProcessor(new HandlerResolver(() => new SomeCommandHandler()));
            
            await processor.ProcessAsync(new SomeCommand());

            Assert.Pass();
        }

        [Test]
        public async Task Should_process_command_with_result()
        {
            const string expectedResult = "test";
            var processor = new CommandProcessor(new HandlerResolver(() => new SomeResultingCommandHandler()));

            var result = await processor.ProcessAsync(new SomeResultingCommand(expectedResult));
           
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_command()
        {
            var pipelineForSomeCommandExecuted = false;
            var pipelineForAnotherCommandExecuted = false;
            
            var processor = new CommandProcessor(new HandlerResolver(() => new SomeCommandHandler()), configurator =>
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

            var processor = new CommandProcessor(new HandlerResolver(() => new SomeCommandHandler()), configurator =>
            {
                configurator.UseForCommands(
                    new[] {typeof(SomeCommand)},
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
        
        private class SomeCommand : ICommand
        {
        }
    
        private class AnotherCommand : ICommand
        {
        }
    
        private class SomeCommandHandler : ICommandHandler<SomeCommand>
        {
            public Task HandleAsync(ICommandContext<SomeCommand> context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    
        private class SomeResultingCommand : IResultingCommand<string>
        {
            public SomeResultingCommand(string someProperty)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }
    
        private class SomeResultingCommandHandler : ICommandHandler<SomeResultingCommand, string>
        {
            public Task<string> HandleAsync(ICommandContext<SomeResultingCommand> context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(context.Command.SomeProperty);
            }
        }
    }
}