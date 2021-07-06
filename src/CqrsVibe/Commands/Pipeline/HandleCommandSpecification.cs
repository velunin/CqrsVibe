using System;
using System.Collections.Generic;
using System.Linq;
using CqrsVibe.ContextAbstractions;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe.Commands.Pipeline
{
    internal class HandleCommandSpecification : IPipeSpecification<ICommandHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;

        public HandleCommandSpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor ?? throw new ArgumentNullException(nameof(resolverAccessor));
        }

        public void Apply(IPipeBuilder<ICommandHandlingContext> builder)
        {
            builder.AddFilter(new InlineFilter<ICommandHandlingContext>((context, next) =>
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
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}