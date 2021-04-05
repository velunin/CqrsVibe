using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries.Pipeline;

namespace CqrsVibe.Queries
{
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> HandleAsync(IQueryContext<TQuery> context, CancellationToken cancellationToken = default);
    }
}