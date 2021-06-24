using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("CqrsVibe.Tests")]

namespace CqrsVibe.FluentValidation
{
    internal static class ValidationResultTaskFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<ValidationState, Type, Task>> TaskCreateInvokersCache =
            new ConcurrentDictionary<Type, Func<ValidationState, Type, Task>>();

        public static Task Create(ValidationState validationState, Type resultType)
        {
            if (!TaskCreateInvokersCache.TryGetValue(resultType, out var taskCreateInvoker))
            {
                taskCreateInvoker = CreateInvoker(resultType);
                TaskCreateInvokersCache.TryAdd(resultType, taskCreateInvoker);
            }

            return taskCreateInvoker(validationState, resultType);
        }
        
        private static Func<ValidationState, Type, Task> CreateInvoker(Type resultType)
        {
            var eitherConstructor = resultType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] {typeof(ValidationState)},
                null);
            
            var validationResultParameter = Expression.Parameter(typeof(ValidationState), "state");
            var validationResultTypeParameter = Expression.Parameter(typeof(Type), "resultType");
            var eitherConstructorCall = Expression.New(eitherConstructor!, validationResultParameter);

            var taskFromResultMethod = typeof(Task).GetMethod("FromResult",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(resultType);

            var fromResultCall = Expression.Call(taskFromResultMethod, eitherConstructorCall);
            var lambda = Expression.Lambda<Func<ValidationState, Type, Task>>(
                fromResultCall, 
                validationResultParameter, 
                validationResultTypeParameter);
            
            return lambda.Compile();
        }
    }
}