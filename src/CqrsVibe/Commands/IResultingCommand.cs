namespace CqrsVibe.Commands
{
    public interface IResultingCommand<out TResult> : ICommand
    {
    }
}