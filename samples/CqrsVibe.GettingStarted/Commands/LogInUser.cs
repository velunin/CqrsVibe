using CqrsVibe.Commands;

namespace GettingStartedApp.Commands
{
    public class LogInUser : ICommand
    {
        public LogInUser(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}