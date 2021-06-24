using System;
using System.Linq;

namespace CqrsVibe.Queries
{
    public interface IQuery
    {
    }
    
    public interface IQuery<out TResult> : IQuery
    {
    }

    public static class QueryExtensions
    {
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
