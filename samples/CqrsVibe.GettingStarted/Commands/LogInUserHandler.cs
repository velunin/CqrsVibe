using System;
using System.Threading;
using System.Threading.Tasks;
using CqrsVibe.Commands;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events;
using GettingStartedApp.Events;

namespace GettingStartedApp.Commands
{
    public class LogInUserHandler : ICommandHandler<LogInUser>
    {
        private readonly IEventDispatcher _eventDispatcher;

        public LogInUserHandler(IEventDispatcher eventDispatcher)
        {
            _eventDispatcher = eventDispatcher;
        }

        public Task HandleAsync(
            ICommandHandlingContext<LogInUser> context,
            CancellationToken cancellationToken = default)
        {
            ExecutionContext.CurrentUser = new User
            {
                Name = context.Command.Name,
                LoggedInAt = DateTime.Now
            };

            _eventDispatcher.DispatchAsync(new UserWasLoggedIn(
                    ExecutionContext.CurrentUser.Name, 
                    ExecutionContext.CurrentUser.LoggedInAt),
                cancellationToken);

            return Task.CompletedTask;
        }
    }
}