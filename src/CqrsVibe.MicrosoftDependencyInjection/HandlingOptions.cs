using System;
using CqrsVibe.Commands.Pipeline;
using CqrsVibe.Events.Pipeline;
using CqrsVibe.Queries.Pipeline;
using GreenPipes;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    public class HandlingOptions
    {
        public Action<IServiceProvider, IPipeConfigurator<ICommandHandlingContext>> CommandsCfg { get; set; }

        public Action<IServiceProvider, IPipeConfigurator<IQueryHandlingContext>> QueriesCfg { get; set; }

        public Action<IServiceProvider, IPipeConfigurator<IEventHandlingContext>> EventsCfg { get; set; }
    }
}