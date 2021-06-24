using System;
using FluentValidation;

namespace CqrsVibe.FluentValidation.Extensions
{
    public static class ResolverExtensions
    {
        public static bool TryResolveValidator(this IDependencyResolver resolver, Type validatorType, out IValidator validator)
        {
            resolver.TryResolveService(validatorType, out var validatorObj);
            validator = validatorObj as IValidator;
            return validator != null;
        }
    }
}