using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    [TestFixture]
    public class QueryProcessingTests
    {
        [Test]
        public async Task Should_execute_query()
        {
            const string expectedResult = "test";
            var queryService = new QueryService(new HandlerResolver(() => new SomeQueryHandler()));

            var result = await queryService.QueryAsync(new SomeQuery(expectedResult));
            
            Assert.AreEqual(expectedResult, result);
        }
        
        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_query()
        {
            var pipelineForSomeQueryExecuted = false;
            var pipelineForAnotherQueryExecuted = false;
            
            var queryService = new QueryService(new HandlerResolver(() => new SomeQueryHandler()), configurator =>
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

            var queryService = new QueryService(new HandlerResolver(() => new SomeQueryHandler()), configurator =>
            {
                configurator.UseForQueries(
                    new[] {typeof(SomeQuery)},
                    cfg =>
                        cfg.UseExecute(_ => pipelineForSomeQueryExecuted = true));

                configurator.UseForQueries(
                    new[] {typeof(AnotherQuery)},
                    cfg =>
                        cfg.UseExecute(_ => pipelineForAnotherQueryExecuted = true));
            });

            await queryService.QueryAsync(new SomeQuery());
            
            Assert.IsTrue(pipelineForSomeQueryExecuted);
            Assert.IsFalse(pipelineForAnotherQueryExecuted);
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

        private class SomeQueryHandler : IQueryHandler<SomeQuery, string>
        {
            public Task<string> HandleAsync(
                IQueryContext<SomeQuery> context, 
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(context.Query.SomeProperty);
            }
        }
    }
}