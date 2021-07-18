using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Events;
using CqrsVibe.Events.Pipeline;

namespace GettingStartedApp.Events.Handlers
{
    public class BeepWhenUserWasLoggedIn : IEventHandler<UserWasLoggedIn>
    {
        public Task HandleAsync(
            IEventHandlingContext<UserWasLoggedIn> context, 
            CancellationToken cancellationToken = default)
        {
            Console.Beep();
            return Task.CompletedTask;
        }
    }
}