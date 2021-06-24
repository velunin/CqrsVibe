using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe.Commands.Pipeline
{
    internal class HandleCommandSpecification : IPipeSpecification<ICommandHandlingContext>
    {
        private readonly IDependencyResolverAccessor _resolverAccessor;
        private readonly HandlerInvokerFactory<ICommandHandlingContext> _commandHandlerInvokerFactory;

        public HandleCommandSpecification(IDependencyResolverAccessor resolverAccessor)
        {
            _resolverAccessor = resolverAccessor ?? throw new ArgumentNullException(nameof(resolverAccessor));
            _commandHandlerInvokerFactory = new HandlerInvokerFactory<ICommandHandlingContext>();
        }

        public void Apply(IPipeBuilder<ICommandHandlingContext> builder)
        {
            builder.AddFilter(new InlineFilter<ICommandHandlingContext>((context, next) =>
            {
                var commandContext = (CommandHandlingContext) context;

                var commandHandlerInvoker = _commandHandlerInvokerFactory.GetOrCreate(
                    commandContext.GetType(), 
                    commandContext.CommandHandlerInterface);

                var commandHandlerInstance = _resolverAccessor.Current.ResolveService(commandHandlerInvoker.HandlerInterface);
                
                commandContext.SetResult(commandHandlerInvoker.HandleAsync(
                    commandHandlerInstance,
                    context,
                    context.CancellationToken));

                return commandContext.Result;
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}