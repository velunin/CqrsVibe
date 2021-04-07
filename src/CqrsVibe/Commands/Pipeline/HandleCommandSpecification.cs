using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;
using GreenPipes.Internals.Extensions;

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

    internal class ConcreteCommandContextConverterFactory : IPipeContextConverterFactory<ICommandHandlingContext>
    {
        public IPipeContextConverter<ICommandHandlingContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            var commandType = typeof(TOutput).GetClosingArguments(typeof(ICommandHandlingContext<>)).Single();
            
            return (IPipeContextConverter<ICommandHandlingContext, TOutput>)Activator
                .CreateInstance(typeof(CommandContextConverter<>)
                    .MakeGenericType(commandType));
        }

        private class CommandContextConverter<T> : 
            IPipeContextConverter<ICommandHandlingContext, ICommandHandlingContext<T>>
            where T : ICommand
        {
            public bool TryConvert(ICommandHandlingContext input, out ICommandHandlingContext<T> output)
            {
                output = input as ICommandHandlingContext<T>;
                return output != null;
            }
        }
    }

    internal class CommandContextConverterFactory : IPipeContextConverterFactory<ICommandHandlingContext>
    {
        private readonly Func<ICommand, bool> _filter;

        public CommandContextConverterFactory(Func<ICommand, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public IPipeContextConverter<ICommandHandlingContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            return (IPipeContextConverter<ICommandHandlingContext, TOutput>)new CommandContextConverter(_filter);
        }
        
        private class CommandContextConverter : IPipeContextConverter<ICommandHandlingContext, ICommandHandlingContext>
        {
            private readonly Func<ICommand, bool> _filter;

            public CommandContextConverter(Func<ICommand, bool> filter)
            {
                _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }

            public bool TryConvert(ICommandHandlingContext input, out ICommandHandlingContext output)
            {
                if (!_filter(input.Command))
                {
                    output = null;
                    return false;
                }
                output = input;
                return true;
            }
        }
    }
}