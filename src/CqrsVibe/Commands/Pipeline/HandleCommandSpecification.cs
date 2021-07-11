using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CqrsVibe.ContextAbstractions;
using GreenPipes;

namespace CqrsVibe.Commands.Pipeline
{
    /// <summary>
    /// Specification for add <see cref="HandleCommandFilter"/> to pipeline
    /// </summary>
    internal class HandleCommandSpecification : IPipeSpecification<ICommandHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleCommandSpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor ?? throw new ArgumentNullException(nameof(resolverAccessor));
        }

        public void Apply(IPipeBuilder<ICommandHandlingContext> builder)
        {
            builder.AddFilter(new HandleCommandFilter(_resolverAccessor));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    /// <summary>
    /// Filter for resolving and invoking command handler
    /// </summary>
    internal class HandleCommandFilter : IFilter<ICommandHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleCommandFilter(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor;
        }

        public Task Send(ICommandHandlingContext context, IPipe<ICommandHandlingContext> next)
        {
            var commandHandlerInvoker = HandlerInvokerFactory<ICommandHandlingContext>.GetOrCreate(
                context.GetType(), 
                context.CommandHandlerInterface);

            var commandHandlerInstance = _resolverAccessor.Current.ResolveService(commandHandlerInvoker.HandlerInterface);

            if (context is IResultingHandlingContext resultingContext)
            {
                resultingContext.SetResultTask(commandHandlerInvoker.HandleAsync(
                    commandHandlerInstance,
                    context,
                    context.CancellationToken));

                return resultingContext.ResultTask;
            }
                
            return commandHandlerInvoker.HandleAsync(
                commandHandlerInstance,
                context,
                context.CancellationToken);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("handleCommand");
        }
    }
}