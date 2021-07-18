using System.Threading.Tasks;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.PipelineConfiguration.Services;
using GreenPipes;

namespace CqrsVibe.PipelineConfiguration.Pipeline
{
    public class SomeCommandFilter : IFilter<ICommandHandlingContext>
    {
        private readonly ISingletonService _service;

        public SomeCommandFilter(ISingletonService service)
        {
            _service = service;
        }

        public async Task Send(ICommandHandlingContext context, IPipe<ICommandHandlingContext> next)
        {
            //Pre-processing logic
            await next.Send(context);
            //Post-processing logic
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("someCustomCommandFilter");
        }
    }
}