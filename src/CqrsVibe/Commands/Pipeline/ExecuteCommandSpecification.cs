using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using GreenPipes.Filters;
using GreenPipes.Internals.Extensions;

namespace CqrsVibe.Commands.Pipeline
{
    internal class ExecuteCommandSpecification : IPipeSpecification<ICommandContext>
    {
        private readonly IHandlerResolver _handlerResolver;
        private readonly HandlerInvokerFactory<ICommandContext> _commandHandlerInvokerFactory;

        public ExecuteCommandSpecification(IHandlerResolver handlerResolver)
        {
            _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
            _commandHandlerInvokerFactory = new HandlerInvokerFactory<ICommandContext>();
        }

        public void Apply(IPipeBuilder<ICommandContext> builder)
        {
            builder.AddFilter(new DelegateFilter<ICommandContext>(context =>
            {
                var commandContext = (CommandContext) context;
      
                var commandHandlerInvoker = _commandHandlerInvokerFactory.GetOrCreate(
                    commandContext.GetType(), 
                    commandContext.CommandHandlerType);

                var commandHandlerInstance = _handlerResolver.ResolveHandler(commandHandlerInvoker.HandlerType);
                
                commandContext.Result = commandHandlerInvoker.HandleAsync(
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

    internal class ConcreteCommandContextConverterFactory : IPipeContextConverterFactory<ICommandContext>
    {
        public IPipeContextConverter<ICommandContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            var commandType = typeof(TOutput).GetClosingArguments(typeof(ICommandContext<>)).Single();
            
            return (IPipeContextConverter<ICommandContext, TOutput>)Activator
                .CreateInstance(typeof(CommandContextConverter<>)
                    .MakeGenericType(commandType));
        }

        private class CommandContextConverter<T> : 
            IPipeContextConverter<ICommandContext, ICommandContext<T>>
            where T : ICommand
        {
            public bool TryConvert(ICommandContext input, out ICommandContext<T> output)
            {
                output = input as ICommandContext<T>;
                return output != null;
            }
        }
    }

    internal class CommandContextConverterFactory : IPipeContextConverterFactory<ICommandContext>
    {
        private readonly Func<ICommand, bool> _filter;

        public CommandContextConverterFactory(Func<ICommand, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public IPipeContextConverter<ICommandContext, TOutput> GetConverter<TOutput>() where TOutput : class, PipeContext
        {
            return (IPipeContextConverter<ICommandContext, TOutput>)new CommandContextConverter(_filter);
        }
        
        private class CommandContextConverter : IPipeContextConverter<ICommandContext, ICommandContext>
        {
            private readonly Func<ICommand, bool> _filter;

            public CommandContextConverter(Func<ICommand, bool> filter)
            {
                _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }

            public bool TryConvert(ICommandContext input, out ICommandContext output)
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