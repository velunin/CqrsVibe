using System;
using System.Linq;

namespace CqrsVibe.FluentValidation.Extensions
{
    public static class EitherExtensions
    {
        public static bool IsValid<TResult>(this Either<TResult, ValidationState> either)
        {
            return either.RightOrDefault().IsValid;
        }

        public static bool TryGetResult<TResult>(this Either<TResult, ValidationState> either, out TResult result)
        {
            result = either.LeftOrDefault();
            return either.IsValid();
        }

        public static void Deconstruct<TResult>(this Either<TResult, ValidationState> either, out TResult result,
            out ValidationState validationResult)
        {
            result = either.LeftOrDefault();
            validationResult = either.RightOrDefault();
        }

        internal static bool IsEitherWithValidationResult(this Type type)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Either<,>))
            {
                return false;
            }

            var eitherRightType = type.GetGenericArguments().Last();
            return eitherRightType == typeof(ValidationState);
        }
    }
}