using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe.Commands.Pipeline
{
    internal class HandleCommandSpecification : IPipeSpecification<ICommandHandlingContext>
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly HandlerInvokerFactory<ICommandHandlingContext> _commandHandlerInvokerFactory;

        public HandleCommandSpecification(IHandlerResolver handlerResolver)
        {
            _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
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

                var commandHandlerInstance = _handlerResolver.ResolveHandler(commandHandlerInvoker.HandlerInterface);
                
                return commandContext.Result = commandHandlerInvoker.HandleAsync(
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