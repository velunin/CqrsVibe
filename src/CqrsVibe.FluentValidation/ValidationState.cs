using System.Collections.Generic;
using FluentValidation.Results;

namespace CqrsVibe.FluentValidation
{
    public readonly struct ValidationState
    {
        private ValidationState(ValidationResult validationResult)
        {
            Errors = validationResult.Errors;
        }

        public static ValidationState ErrorState(ValidationResult validationResult)
        {
            return new ValidationState(validationResult);
        }

        public List<ValidationFailure> Errors { get; }

        public bool IsValid => Errors == null || Errors.Count == 0;
    }
}