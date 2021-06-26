using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using Moq;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class QueryProcessingTests : BaseTest
    {
        [Test]
        public async Task Should_execute_query()
        {
            const string expectedResult = "test";
            var queryService = new QueryService(ResolverAccessor);

            var result = await queryService.QueryAsync(new SomeQuery(expectedResult));

            Assert.AreEqual(expectedResult, result);
        }
        
        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_query()
        {
            var pipelineForSomeQueryExecuted = false;
            var pipelineForAnotherQueryExecuted = false;
            var queryService = new QueryService(ResolverAccessor, configurator =>
            {
                configurator.UseForQuery<SomeQuery>(cfg =>
                    cfg.UseExecute(_ => pipelineForSomeQueryExecuted = true));
                
                configurator.UseForQuery<AnotherQuery>(cfg =>
                    cfg.UseExecute(_ => pipelineForAnotherQueryExecuted = true));
            });

            await queryService.QueryAsync(new SomeQuery(string.Empty));
            
            Assert.IsTrue(pipelineForSomeQueryExecuted);
            Assert.IsFalse(pipelineForAnotherQueryExecuted);
        }
        
        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_query_types()
        {
            var pipelineForSomeQueryExecuted = false;
            var pipelineForAnotherQueryExecuted = false;
            var queryService = new QueryService(ResolverAccessor, cfg =>
            {
                cfg.UseForQueries(
                    new[] {typeof(SomeQuery)}.ToHashSet(),
                    cfg2 =>
                        cfg2.UseExecute(_ => pipelineForSomeQueryExecuted = true));

                cfg.UseForQueries(
                    new[] {typeof(AnotherQuery)},
                    cfg2 =>
                        cfg2.UseExecute(_ => pipelineForAnotherQueryExecuted = true));
            });

            await queryService.QueryAsync(new SomeQuery());
            
            Assert.IsTrue(pipelineForSomeQueryExecuted);
            Assert.IsFalse(pipelineForAnotherQueryExecuted);
        }

        [Test]
        public void Should_throw_correct_exception()
        {
            const string expectedResult = "test";
            var queryService = new QueryService(ResolverAccessor);

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return queryService.QueryAsync(new SomeBuggyQuery(expectedResult));
            });
            
            Assert.AreEqual(expectedResult, exception.Message);
        }

        [Test]
        public async Task Should_process_pipeline_for_specified_query_in_correct_order()
        {
            var traceMock = new Mock<ITraceService>(MockBehavior.Strict);
            var s = new MockSequence();
            var queryService = new QueryService(ResolverAccessor, cfg =>
            {
                cfg.UseForQuery<SomeQuery>(c =>
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
                    return next.Send(context);
                });
            });

            traceMock.InSequence(s).Setup(m => m.BeforeHandle());
            traceMock.InSequence(s).Setup(m => m.Handle());
            traceMock.InSequence(s).Setup(m => m.AfterHandle());

            await queryService.QueryAsync(new SomeQuery());

            traceMock.Verify(m=>m.BeforeHandle());
            traceMock.Verify(m=>m.Handle());
            traceMock.Verify(m=>m.AfterHandle());
        }

        private class SomeQuery : IQuery<string>
        {
            public SomeQuery(string someProperty = null)
            {
                SomeProperty = someProperty;
            }

            public string SomeProperty { get; }
        }
        
        private class AnotherQuery : IQuery<string>
        {
        }

        // ReSharper disable once UnusedType.Local
        private class SomeQueryHandler : IQueryHandler<SomeQuery, string>
        {
            public Task<string> HandleAsync(
                IQueryHandlingContext<SomeQuery> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(context.Query.SomeProperty);
            }
        }

        private class SomeBuggyQuery : IQuery<string>
        {
            public SomeBuggyQuery(string exceptionText)
            {
                ExceptionText = exceptionText;
            }

            public string ExceptionText { get; }
        }

        // ReSharper disable once UnusedType.Local
        private class SomeBuggyQueryHandler : IQueryHandler<SomeBuggyQuery, string>
        {
            public Task<string> HandleAsync(
                IQueryHandlingContext<SomeBuggyQuery> context, 
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException(context.Query.ExceptionText);
            }
        }
    }
}