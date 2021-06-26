using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.ContextAbstractions;

namespace CqrsVibe.Queries.Pipeline
{
    public interface IQueryHandlingContext : IResultingHandlingContext
    {
        IQuery Query { get; }
    }

    public interface IQueryHandlingContext<out TQuery> : IQueryHandlingContext where TQuery : IQuery
    {
        new TQuery Query { get; }
    }
    
    internal abstract class QueryHandlingContext : BaseHandlingContext, IQueryHandlingContext
    {
        protected QueryHandlingContext(IQuery query, Type queryHandlerInterface, CancellationToken cancellationToken) :
            base(cancellationToken)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            QueryHandlerInterface =
                queryHandlerInterface ?? throw new ArgumentNullException(nameof(queryHandlerInterface));
        }

        public abstract void SetResultTask(Task result);

        public abstract void SetResult(object result);
        
        public abstract Task<object> ExtractResult();

        public IQuery Query { get; }

        public Type QueryHandlerInterface { get; }

        public abstract Task ResultTask { get; }
    }

    internal class QueryHandlingContext<TQuery,TResult> : QueryHandlingContext, 
        IQueryHandlingContext<TQuery> 
        where TQuery : IQuery<TResult>
    {
        private Task<TResult> _resultContainer;

        public QueryHandlingContext(TQuery query, Type queryHandlerInterface, CancellationToken cancellationToken) 
            : base(query, queryHandlerInterface, cancellationToken)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public new TQuery Query { get; }

        public override void SetResult(object result)
        {
            _resultContainer = Task.FromResult((TResult) result);
        }

        public override void SetResultTask(Task result)
        {
            _resultContainer = (Task<TResult>) result;
        }

        public override Task<object> ExtractResult()
        {
            return _resultContainer.ContinueWith(x => (object) x.Result);
        }

        public override Task ResultTask => _resultContainer;
    }
}