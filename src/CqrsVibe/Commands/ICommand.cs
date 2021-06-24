using System;
using System.Linq;

namespace CqrsVibe.Commands
{
    public interface ICommand
    {
    }
    
    public interface ICommand<out TResult> : ICommand
    {
    }

    public static class CommandExtensions
    {
        public static bool TryGetResultType(this ICommand command, out Type resultType)
        {
            resultType = null;
            
            var resultingCommandType = command
                .GetType()
                .GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommand<>));

            if (resultingCommandType == null)
            {
                return false;
            }

            resultType = resultingCommandType.GetGenericArguments().First();
            return true;
        }
    }
}