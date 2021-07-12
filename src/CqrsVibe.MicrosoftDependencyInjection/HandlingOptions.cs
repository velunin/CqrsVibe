using System;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    /// <summary>
    /// CqrsVibe handling options
    /// </summary>
    public class HandlingOptions
    {
        /// <summary>
        /// Delegate for configuring command handling pipeline
        /// </summary>
        public Action<IServiceProvider, IPipeConfigurator<ICommandHandlingContext>> CommandsCfg { get; set; }

        /// <summary>
        /// Delegate for configuring query handling pipeline
        /// </summary>
        public Action<IServiceProvider, IPipeConfigurator<IQueryHandlingContext>> QueriesCfg { get; set; }

        /// <summary>
        /// Delegate for configuring event handling pipeline
        /// </summary>
        public Action<IServiceProvider, IPipeConfigurator<IEventHandlingContext>> EventsCfg { get; set; }
    }
}