using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.FluentValidation.Extensions
{
    public static class ConfiguratorExtensions
    {
        public static void UseFluentValidation(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            OnFailureBehavior behavior)
        {
            configurator.AddPipeSpecification(new ValidationSpecification(behavior));
        }
        
        public static void UseFluentValidation(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            OnFailureBehavior behavior)
        {
            configurator.AddPipeSpecification(new ValidationSpecification(behavior));
        }
    }
}