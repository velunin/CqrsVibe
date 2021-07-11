using System.Threading.Tasks;

namespace CqrsVibe.ContextAbstractions
{
    /// <summary>
    /// Base context for requests that imply a result
    /// </summary>
    public interface IResultingHandlingContext : IHandlingContext
    {
        /// <summary>
        /// The task of getting the result
        /// </summary>
        Task ResultTask { get; }

        /// <summary>
        /// Extract result as object
        /// </summary>
        Task<object> ExtractResult();

        /// <summary>
        /// Set the task of getting the result
        /// </summary>
        /// <param name="result"></param>
        void SetResultTask(Task result);

        /// <summary>
        /// Set result value
        /// </summary>
        void SetResult(object result);
    }
}