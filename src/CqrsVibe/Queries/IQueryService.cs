using System.Threading;
using System.Threading.Tasks;

namespace CqrsVibe.Queries
{
    public interface IQueryService
    {
        Task<TResult> QueryAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}