using System;
using System.Threading.Tasks;
using CqrsVibe.FluentValidation.Extensions;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;
using FluentValidation;
using GreenPipes;

namespace CqrsVibe.FluentValidation
{
    /// <summary>
    /// Query validation filter
    /// </summary>
    internal class QueryValidationFilter : IFilter<IQueryHandlingContext>
    {
        private readonly OnFailureBehavior _behavior;

        public QueryValidationFilter(OnFailureBehavior behavior)
        {
            _behavior = behavior;
        }

        public async Task Send(IQueryHandlingContext context, IPipe<IQueryHandlingContext> next)
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
                    
                        context.SetResult(Activator.CreateInstance(resultType, validationState));
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
            var scope = context.CreateFilterScope("queryValidation");
            scope.Add("onFailureBehavior", _behavior.ToString("G"));
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