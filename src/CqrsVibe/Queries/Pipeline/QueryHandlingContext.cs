using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;

namespace CqrsVibe.Queries.Pipeline
{
    public interface IQueryHandlingContext : IHandlingContext
    {
        IQuery Query { get; }
    }

    public interface IQueryHandlingContext<out TQuery> : IQueryHandlingContext where TQuery : IQuery
    {
        new TQuery Query { get; }
    }
    
    internal class QueryHandlingContext : BaseHandlingContext, IQueryHandlingContext
    {
        protected QueryHandlingContext(IQuery query, Type queryHandlerInterface, CancellationToken cancellationToken) : base(cancellationToken)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            QueryHandlerInterface = queryHandlerInterface ?? throw new ArgumentNullException(nameof(queryHandlerInterface));
        }

        public IQuery Query { get; }

        public Type QueryHandlerInterface { get; }

        public Task Result { get; set; }
    }

    internal class QueryHandlingContext<TQuery> : QueryHandlingContext, IQueryHandlingContext<TQuery> where TQuery : IQuery
    {
        public QueryHandlingContext(TQuery query, Type queryHandlerInterface, CancellationToken cancellationToken) 
            : base(query, queryHandlerInterface, cancellationToken)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public new TQuery Query { get; }
    }
}