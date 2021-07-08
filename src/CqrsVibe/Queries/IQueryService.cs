using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Queries
{
    public interface IQueryService : IProbeSite
    {
        Task<TResult> QueryAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}