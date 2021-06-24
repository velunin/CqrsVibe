using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.FluentValidation;
using CqrsVibe.FluentValidation.Extensions;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using FluentValidation;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class FluentValidationTests : BaseTest
    {
        [Test]
        public void Should_throw_validation_exception_when_rule_violated()
        {
            var commandProcessor = new CommandProcessor(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(OnFailureBehavior.ThrowException);
            });
            var queryService = new QueryService(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(OnFailureBehavior.ThrowException);
            });

            Assert.ThrowsAsync<ValidationException>(() => commandProcessor.ProcessAsync(new TestCommand(null)));
            Assert.ThrowsAsync<ValidationException>(() => queryService.QueryAsync(new TestQuery(null)));
        }

        [Test]
        public void Should_throw_exception_when_wrong_result_type()
        {
            var commandProcessor = new CommandProcessor(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(OnFailureBehavior.ReturnEither);
            });
            var queryService = new QueryService(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(OnFailureBehavior.ReturnEither);
            });

            Assert.ThrowsAsync<InvalidOperationException>(() => commandProcessor.ProcessAsync(new TestCommand("some")));
            Assert.ThrowsAsync<InvalidOperationException>(() => queryService.QueryAsync(new TestQuery("some")));
        }
        
        [Test]
        [TestCase(OnFailureBehavior.ThrowException)]
        [TestCase(OnFailureBehavior.ReturnEither)]
        public async Task Should_pass_happy_path_when_request_valid(OnFailureBehavior behavior)
        {
            var commandProcessor = new CommandProcessor(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(behavior);
            });

            var queryService = new QueryService(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(behavior);
            });

            switch (behavior)
            {
                case OnFailureBehavior.ThrowException:

                    Assert.DoesNotThrowAsync(() => commandProcessor.ProcessAsync(new TestCommand("some")));
                    Assert.DoesNotThrowAsync(() => queryService.QueryAsync(new TestQuery("some")));

                    break;
                case OnFailureBehavior.ReturnEither:
                    var (commandResult, commandValidationState) =
                        await commandProcessor.ProcessAsync(new TestCommandWithEitherResult("some"));

                    var (queryResult, queryValidationState) =
                        await queryService.QueryAsync(new TestQueryWithEitherResult("some"));

                    Assert.IsTrue(commandValidationState.IsValid);
                    Assert.IsTrue(queryValidationState.IsValid);
                    Assert.IsNotEmpty(commandResult);
                    Assert.IsNotEmpty(queryResult);

                    break;
            }
        }

        [Test]
        public async Task Should_return_result_with_validation_failures()
        {
            var commandProcessor = new CommandProcessor(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(OnFailureBehavior.ReturnEither);
            });
            var queryService = new QueryService(ResolverAccessor, cfg =>
            {
                cfg.UseFluentValidation(OnFailureBehavior.ReturnEither);
            });

            var (commandResult, commandValidationState) =
                await commandProcessor.ProcessAsync(new TestCommandWithEitherResult(null));

            var (queryResult, queryValidationState) =
                await queryService.QueryAsync(new TestQueryWithEitherResult(null));

            Assert.IsFalse(commandValidationState.IsValid);
            Assert.IsFalse(queryValidationState.IsValid);
            Assert.IsNotEmpty(commandValidationState.Errors);
            Assert.IsNotEmpty(queryValidationState.Errors);
            Assert.IsNull(commandResult);
            Assert.IsNull(queryResult);
        }

        #region Commands
        public class TestCommand : ICommand<string>
        {
            public TestCommand(string someProperty)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }

        public class TestCommandValidator : AbstractValidator<TestCommand>
        {
            public TestCommandValidator()
            {
                RuleFor(x => x.SomeProperty).NotNull();
            }
        }

        // ReSharper disable once UnusedType.Local
        private class TestCommandHandler : ICommandHandler<TestCommand, string>
        {
            public Task<string> HandleAsync(
                ICommandHandlingContext<TestCommand> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult("Some result");
            }
        }
        
        public class TestCommandWithEitherResult : ICommand<Either<string,ValidationState>>
        {
            public TestCommandWithEitherResult(string someProperty)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }
        
        // ReSharper disable once UnusedType.Global
        public class TestCommandWithEitherResultValidator : AbstractValidator<TestCommandWithEitherResult>
        {
            public TestCommandWithEitherResultValidator()
            {
                RuleFor(x => x.SomeProperty).NotNull();
            }
        }

        // ReSharper disable once UnusedType.Local
        private class TestCommandWithEitherResultHandler : 
            ICommandHandler<TestCommandWithEitherResult, Either<string, ValidationState>>
        {
            public Task<Either<string, ValidationState>> HandleAsync(
                ICommandHandlingContext<TestCommandWithEitherResult> context,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Either<string, ValidationState>("Some result"));
            }
        }
        #endregion

        #region Queries
        public class TestQuery : IQuery<string>
        {
            public TestQuery(string someProperty)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }

        // ReSharper disable once UnusedType.Global
        public class TestQueryValidator : AbstractValidator<TestQuery>
        {
            public TestQueryValidator()
            {
                RuleFor(x => x.SomeProperty).NotNull();
            }
        }

        // ReSharper disable once UnusedType.Local
        private class TestQueryHandler : IQueryHandler<TestQuery, string>
        {
            public Task<string> HandleAsync(
                IQueryHandlingContext<TestQuery> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult("Some result");
            }
        }

        public class TestQueryWithEitherResult : IQuery<Either<string, ValidationState>>
        {
            public TestQueryWithEitherResult(string someProperty)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }

        // ReSharper disable once UnusedType.Global
        public class TestQueryWithEitherResultValidator : AbstractValidator<TestQueryWithEitherResult>
        {
            public TestQueryWithEitherResultValidator()
            {
                RuleFor(x => x.SomeProperty).NotNull();
            }
        }

        // ReSharper disable once UnusedType.Local
        private class TestQueryWithEitherResultHandler : 
            IQueryHandler<TestQueryWithEitherResult, Either<string,ValidationState>>
        {
            public Task<Either<string,ValidationState>> HandleAsync(
                IQueryHandlingContext<TestQueryWithEitherResult> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new Either<string, ValidationState>("Some result"));
            }
        }
        #endregion
    }
}