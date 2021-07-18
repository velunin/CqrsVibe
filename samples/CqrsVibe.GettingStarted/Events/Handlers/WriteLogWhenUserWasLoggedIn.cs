using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events;
using CqrsVibe.Events.Pipeline;

namespace GettingStartedApp.Events.Handlers
{
    public class WriteLogWhenUserWasLoggedIn : IEventHandler<UserWasLoggedIn>
    {
        public async Task HandleAsync(
            IEventHandlingContext<UserWasLoggedIn> context, 
            CancellationToken cancellationToken = default)
        {
            await Console.Out.WriteLineAsync(
                $"----------> User '{context.Event.Name}' was logged in at {context.Event.LoggedInAt}");
        }
    }
}