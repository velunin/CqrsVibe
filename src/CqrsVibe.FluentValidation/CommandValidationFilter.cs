using System;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.ContextAbstractions;
using CqrsVibe.FluentValidation.Extensions;
using FluentValidation;
using GreenPipes;

namespace CqrsVibe.FluentValidation
{
    /// <summary>
    /// Command validation filter
    /// </summary>
    internal class CommandValidationFilter : IFilter<ICommandHandlingContext>
    {
        private readonly OnFailureBehavior _behavior;

        public CommandValidationFilter(OnFailureBehavior behavior)
        {
            _behavior = behavior;
        }

        public async Task Send(ICommandHandlingContext context, IPipe<ICommandHandlingContext> next)
        {
            var commandValidatorType = typeof(IValidator<>).MakeGenericType(context.Command.GetType());

            if(!context.ContextServices.TryResolveValidator(commandValidatorType, out var validator))
            {
                await next.Send(context);
                return;
            }

            ValidateBehavior(context);

            var validationContext = new ValidationContext<ICommand>(context.Command);
            var validationResult = await validator.ValidateAsync(
                validationContext, 
                context.CancellationToken);

            switch (_behavior)
            {
                case OnFailureBehavior.ThrowException:
                    if (!validationResult.IsValid)
                    {
                        throw new ValidationException(validationResult.Errors);
                    }
                    break;
                case OnFailureBehavior.ReturnEither:
                    if (!validationResult.IsValid)
                    {
                        var validationState = ValidationState.ErrorState(validationResult);

                        context.Command.TryGetResultType(out var resultType);

                        ((IResultingHandlingContext)context).SetResult(Activator.CreateInstance(resultType, validationState));

                        return;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Undefined behavior");
            }
                
            await next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("commandValidation");
            scope.Add("onFailureBehavior", _behavior.ToString("G"));
        }

        private void ValidateBehavior(ICommandHandlingContext context)
        {
            if (_behavior == OnFailureBehavior.ReturnEither)
            {
                if (!context.Command.TryGetResultType(out var commandResultType) ||
                    !commandResultType.IsEitherWithValidationResult())
                {
                    throw new InvalidOperationException(
                        $"For mode with '{OnFailureBehavior.ReturnEither:G}' behavior " +
                        "command result type must be Either<TResult,ValidationState>");
                }
            }
        }
    }
}