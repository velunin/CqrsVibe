using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Queries;
using CqrsVibe.Queries.Pipeline;

namespace GettingStartedApp.Queries
{
    public class GetGreetingForCurrentUserHandler : IQueryHandler<GetGreetingForCurrentUser, string>
    {
        public Task<string> HandleAsync(
            IQueryHandlingContext<GetGreetingForCurrentUser> context, 
            CancellationToken cancellationToken = default)
        {
            var greeting = $"Hi, {ExecutionContext.CurrentUser.Name}!";
            return Task.FromResult(greeting);
        }
    }
}