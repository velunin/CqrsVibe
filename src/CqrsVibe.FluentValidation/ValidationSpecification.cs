using System;
using System.Collections.Generic;
using System.Linq;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.FluentValidation.Extensions;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using FluentValidation;
using GreenPipes;
using GreenPipes.Filters;

namespace CqrsVibe.FluentValidation
{
    public class ValidationSpecification : 
        IPipeSpecification<ICommandHandlingContext>, 
        IPipeSpecification<IQueryHandlingContext>
    {
        private readonly OnFailureBehavior _behavior;

        public ValidationSpecification(OnFailureBehavior behavior)
        {
            _behavior = behavior;
        }

        public void Apply(IPipeBuilder<ICommandHandlingContext> builder)
        {
            builder.AddFilter(new InlineFilter<ICommandHandlingContext>(async (context, next) =>
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

                            context.SetResult(
                                ValidationResultTaskFactory.Create(validationState, resultType));

                            return;
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Undefined behavior");
                }
                
                await next.Send(context);
            }));
        }

        public void Apply(IPipeBuilder<IQueryHandlingContext> builder)
        {
            builder.AddFilter(new InlineFilter<IQueryHandlingContext>(async (context, next) =>
            {
                var commandValidatorType = typeof(IValidator<>).MakeGenericType(context.Query.GetType());

                if(!context.ContextServices.TryResolveValidator(commandValidatorType, out var validator))
                {
                    await next.Send(context);
                    return;
                }

                ValidateBehavior(context);

                var validationContext = new ValidationContext<IQuery>(context.Query);
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

                            context.Query.TryGetResultType(out var resultType);

                            context.SetResult(
                                ValidationResultTaskFactory.Create(validationState, resultType));

                            return;
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Undefined behavior");
                }
                
                await next.Send(context);
            }));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
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

        private void ValidateBehavior(IQueryHandlingContext context)
        {
            if (_behavior == OnFailureBehavior.ReturnEither)
            {
                if (!context.Query.TryGetResultType(out var commandResultType) ||
                    !commandResultType.IsEitherWithValidationResult())
                {
                    throw new InvalidOperationException(
                        $"For mode with '{OnFailureBehavior.ReturnEither:G}' behavior " +
                        "query result type must be Either<TResult,ValidationState>");
                }
            }
        }
    }
}