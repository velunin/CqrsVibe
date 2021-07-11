using System;
using System.Linq;

namespace CqrsVibe.Commands
{
    /// <summary>
    /// Base interface for all commands
    /// </summary>
    public interface ICommand
    {
    }

    //Base interface for all commands with result
    public interface ICommand<out TResult> : ICommand
    {
    }

    /// <summary>
    /// Command extensions
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Gets the result type of command
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="resultType">Command result type</param>
        /// <returns>True if command has result</returns>
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