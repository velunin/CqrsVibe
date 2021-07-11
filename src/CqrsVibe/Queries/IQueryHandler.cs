using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries.Pipeline;

namespace CqrsVibe.Queries
{
    /// <summary>
    /// Base interface for all query handlers
    /// </summary>
    /// <typeparam name="TQuery">Query to handle</typeparam>
    /// <typeparam name="TResult">Query result type</typeparam>
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handle query
        /// </summary>
        /// <param name="context">Query handling context</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Query result</returns>
        Task<TResult> HandleAsync(IQueryHandlingContext<TQuery> context, CancellationToken cancellationToken = default);
    }
}