# CqrsVibe 

[![build](https://github.com/velunin/CqrsVibe/actions/workflows/ci.yml/badge.svg)](https://github.com/velunin/CqrsVibe/actions/workflows/ci.yml)
[![nuget version](https://img.shields.io/nuget/v/CqrsVibe?label=nuget)](https://www.nuget.org/packages/CqrsVibe)
![license](https://img.shields.io/github/license/velunin/cqrsvibe)  

CqrsVibe is an implementation of CQRS with pipelines via [GreenPipes](https://github.com/phatboyg/GreenPipes)

## Getting started
Install [CqrsVibe package with Microsoft DI](https://www.nuget.org/packages/CqrsVibe.MicrosoftDependencyInjection/) abstractions support

```Install-Package CqrsVibe.MicrosoftDependencyInjection``` 

Register and configure CqrsVibe services:
```c#
services.AddCqrsVibe(options =>
{
    options.CommandsCfg = (provider, cfg) =>
    {
        //Commands handling pipeline configuration
        cfg.UseInlineFilter(async (context, next) =>
        {
            //Pre-processing logic

            await next.Send(context);

            //Post-processing logic
        });
    };
    options.QueriesCfg = (provider, cfg) =>
    {
        //Queries handling pipeline configuration
    };
    options.EventsCfg = (provider, cfg) =>
    {
        //Events handling pipeline configuration
    };
});
```
Register handlers:
```c#
services.AddCqrsVibeHandlers(
    fromAssemblies:new []
    {
        typeof(SomeCommand).Assembly
    });
```
## Usage
### Commands processing
Use `ProcessAsync` method of `ICommandProcessor` for performing command
```c#
//without result
await commandProcessor.ProcessAsync(new SomeCommand(), cancellationToken);

//with result
var result = await commandProcessor.ProcessAsync(new SomeCommandWithResult(), cancellationToken);
```
#### Command
```c#
public class SomeCommand : ICommand
{
}

public class SomeCommandHandler : ICommandHandler<SomeCommand>
{
    public Task HandleAsync(
        ICommandHandlingContext<SomeCommand> context, 
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```
#### Command with result
```c#
public class SomeCommandWithResult : ICommand<string>
{
}

public class class SomeCommandWithResultHandler : ICommandHandler<SomeCommandWithResult, string>
{
    public Task<string> HandleAsync(
        ICommandHandlingContext<SomeCommandWithResult> context, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Some command result");
    }
}
```
### Queries execution
Call the `QueryAsync` method of `IQueryService` to execute your queries
```c#
var result = await queryService.QueryAsync(new SomeQuery(), cancellationToken);
```
#### Query sample
```c#
public class SomeQuery : IQuery<string>
{
    public SomeQuery(string someQueryParam)
    {
        SomeQueryParam = someQueryParam;
    }

    public string SomeQueryParam { get; }
}

public class SomeQueryHandler : IQueryHandler<SomeQuery, string>
{
    public Task<string> HandleAsync(
        IQueryHandlingContext<SomeQuery> context, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Some query result");
    }
}
```

More usage examples: https://github.com/velunin/CqrsVibe/tree/master/samples
