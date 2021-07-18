using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;

namespace CqrsVibe.PipelineConfiguration
{
    public class SomeQueryHandler : IQueryHandler<SomeQuery, string>
    {
        public Task<string> HandleAsync(
            IQueryHandlingContext<SomeQuery> context, 
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Some result");
        }
    }
}