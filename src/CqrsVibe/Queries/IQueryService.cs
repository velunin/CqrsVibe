using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Queries
{
    public interface IQueryService : IProbeSite
    {
        /// <summary>
        /// Executes query
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TResult">Query result type</typeparam>
        /// <returns>Query result</returns>
        Task<TResult> QueryAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}