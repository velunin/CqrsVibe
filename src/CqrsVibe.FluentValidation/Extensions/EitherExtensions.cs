using System;
using System.Linq;

namespace CqrsVibe.FluentValidation.Extensions
{
    /// <summary>
    /// Either extensions
    /// </summary>
    public static class EitherExtensions
    {
        /// <summary>
        /// Either has a valid state
        /// </summary>
        /// <param name="either"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static bool IsValid<TResult>(this Either<TResult, ValidationState> either)
        {
            return either.RightOrDefault().IsValid;
        }

        /// <summary>
        /// Try to get a result
        /// </summary>
        /// <param name="either"></param>
        /// <param name="result"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static bool TryGetResult<TResult>(this Either<TResult, ValidationState> either, out TResult result)
        {
            result = either.LeftOrDefault();
            return either.IsValid();
        }

        /// <summary>
        /// Deconstruct to ValueTuple
        /// </summary>
        /// <param name="either"></param>
        /// <param name="result"></param>
        /// <param name="validationResult"></param>
        /// <typeparam name="TResult"></typeparam>
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