using System;
using System.Linq;

namespace CqrsVibe.Queries
{
    /// <summary>
    /// Marker interface for queries
    /// </summary>
    public interface IQuery
    {
    }

    /// <summary>
    /// Base interface for all queries
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IQuery<out TResult> : IQuery
    {
    }

    /// <summary>
    /// Query extensions
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Gets the result type of query
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="resultType">Query result type</param>
        /// <returns>True if query has result</returns>
        public static bool TryGetResultType(this IQuery query, out Type resultType)
        {
            resultType = null;
            
            var queryType = query
                .GetType()
                .GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IQuery<>));

            if (queryType == null)
            {
                return false;
            }

            resultType = queryType.GetGenericArguments().First();
            return true;
        }
    }
}
