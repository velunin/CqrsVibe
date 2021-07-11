using System.Collections.Generic;
using FluentValidation.Results;

namespace CqrsVibe.FluentValidation
{
    /// <summary>
    /// State of validation
    /// </summary>
    public readonly struct ValidationState
    {
        private ValidationState(ValidationResult validationResult)
        {
            Errors = validationResult.Errors;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="ValidationState"/> in error state
        /// </summary>
        /// <param name="validationResult"></param>
        /// <returns></returns>
        public static ValidationState ErrorState(ValidationResult validationResult)
        {
            return new ValidationState(validationResult);
        }

        /// <summary>
        /// Validation failures
        /// </summary>
        public List<ValidationFailure> Errors { get; }

        /// <summary>
        /// True when the state is valid
        /// </summary>
        public bool IsValid => Errors == null || Errors.Count == 0;
    }
}