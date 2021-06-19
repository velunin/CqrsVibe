﻿using System;
using System.Linq;
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
        private readonly IDependencyResolverAccessor _resolverAccessor =
            new DependencyResolverAccessor(null);

        [Test]
        public async Task Should_execute_query()
        {
            const string expectedResult = "test";

            _resolverAccessor.Current = new DependencyResolver(() => new SomeQueryHandler());

            var queryService = new QueryService(_resolverAccessor);

            var result = await queryService.QueryAsync(new SomeQuery(expectedResult));
            
            Assert.AreEqual(expectedResult, result);
        }
        
        [Test]
        public async Task Should_execute_configured_pipeline_for_specific_query()
        {
            var pipelineForSomeQueryExecuted = false;
            var pipelineForAnotherQueryExecuted = false;

            _resolverAccessor.Current = new DependencyResolver(() => new SomeQueryHandler());

            var queryService = new QueryService(_resolverAccessor, configurator =>
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

            _resolverAccessor.Current = new DependencyResolver(() => new SomeQueryHandler());

            var queryService = new QueryService(_resolverAccessor, configurator =>
            {
                configurator.UseForQueries(
                    new[] {typeof(SomeQuery)}.ToHashSet(),
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

        [Test]
        public void Should_throw_correct_exception()
        {
            const string expectedResult = "test";

            _resolverAccessor.Current = new DependencyResolver(() => new SomeBuggyQueryHandler());

            var queryService = new QueryService(_resolverAccessor);

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return queryService.QueryAsync(new SomeBuggyQuery(expectedResult));
            });
            
            Assert.AreEqual(expectedResult, exception.Message);
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