using System;
using FluentValidation;

namespace CqrsVibe.FluentValidation.Extensions
{
    /// <summary>
    /// IDependencyResolver extensions
    /// </summary>
    public static class ResolverExtensions
    {
        /// <summary>
        /// Try to resolve registered validator
        /// </summary>
        /// <param name="resolver"></param>
        /// <param name="validatorType"></param>
        /// <param name="validator"></param>
        /// <returns></returns>
        public static bool TryResolveValidator(this IDependencyResolver resolver, Type validatorType, out IValidator validator)
        {
            resolver.TryResolveService(validatorType, out var validatorObj);
            validator = validatorObj as IValidator;
            return validator != null;
        }
    }
}