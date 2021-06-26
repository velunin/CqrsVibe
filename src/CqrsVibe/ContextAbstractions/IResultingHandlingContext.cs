using System.Threading.Tasks;

namespace CqrsVibe.ContextAbstractions
{
    public interface IResultingHandlingContext : IHandlingContext
    {
        Task ResultTask { get; }
        
        Task<object> ExtractResult();
        
        void SetResultTask(Task result);
        
        void SetResult(object result);
    }
}