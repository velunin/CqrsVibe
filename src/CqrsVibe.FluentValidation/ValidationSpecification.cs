using System.Collections.Generic;
using System.Linq;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.FluentValidation
{
    /// <summary>
    /// Specification for add command validation filter and query validation filter
    /// </summary>
    internal class ValidationSpecification : 
        IPipeSpecification<ICommandHandlingContext>, 
        IPipeSpecification<IQueryHandlingContext>
    {
        private readonly OnFailureBehavior _behavior;

        public ValidationSpecification(OnFailureBehavior behavior)
        {
            _behavior = behavior;
        }

        public void Apply(IPipeBuilder<ICommandHandlingContext> builder)
        {
            builder.AddFilter(new CommandValidationFilter(_behavior));
        }

        public void Apply(IPipeBuilder<IQueryHandlingContext> builder)
        {
            builder.AddFilter(new QueryValidationFilter(_behavior));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }
}