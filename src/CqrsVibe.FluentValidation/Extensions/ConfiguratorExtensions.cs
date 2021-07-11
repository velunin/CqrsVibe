using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.FluentValidation.Extensions
{
    public static class ConfiguratorExtensions
    {
        /// <summary>
        /// Configure commands validation
        /// </summary>
        /// <param name="configurator">Pipeline configurator</param>
        /// <param name="behavior">Behavior on failure</param>
        public static void UseFluentValidation(
            this IPipeConfigurator<ICommandHandlingContext> configurator, 
            OnFailureBehavior behavior)
        {
            configurator.AddPipeSpecification(new ValidationSpecification(behavior));
        }

        /// <summary>
        /// Configure queries validation
        /// </summary>
        /// <param name="configurator">Pipeline configurator</param>
        /// <param name="behavior">Behavior on failure</param>
        public static void UseFluentValidation(
            this IPipeConfigurator<IQueryHandlingContext> configurator, 
            OnFailureBehavior behavior)
        {
            configurator.AddPipeSpecification(new ValidationSpecification(behavior));
        }
    }
}