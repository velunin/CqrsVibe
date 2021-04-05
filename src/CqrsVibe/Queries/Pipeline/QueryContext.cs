using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Queries.Pipeline
{
    public interface IQueryContext : PipeContext
    {
        IQuery Query { get; }
    }

    public interface IQueryContext<out TQuery> : IQueryContext where TQuery : IQuery
    {
        new TQuery Query { get; }
    }
    
    internal class QueryContext : BasePipeContext, IQueryContext
    {
        protected QueryContext(IQuery query, Type handlerType, CancellationToken cancellationToken) : base(cancellationToken)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            QueryHandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        }

        public IQuery Query { get; }

        public Type QueryHandlerType { get; }

        public Task Result { get; set; }
    }

    internal class QueryContext<TQuery> : QueryContext, IQueryContext<TQuery> where TQuery : IQuery
    {
        public QueryContext(TQuery query, Type handlerType, CancellationToken cancellationToken) 
            : base(query, handlerType, cancellationToken)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public new TQuery Query { get; }
    }
}