using System.Threading.Tasks;
using CqrsVibe.PipelineConfiguration.Services;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.PipelineConfiguration.Pipeline
{
    public class SomeQueriesMiddleware
    {
        private readonly ISingletonService _service; 

        public SomeQueriesMiddleware(ISingletonService service)
        {
            _service = service;
        }

        public Task Invoke(
            IQueryHandlingContext context,     //Context. Required argument
            IPipe<IQueryHandlingContext> next, //Next filter in pipeline. Required argument,
            IScopedService scopedService       //Scoped dependency. Will be resolved from dependency resolver of current scope.
                                               //Optional argument
            )
        {
            return next.Send(context);
        }
    }
}