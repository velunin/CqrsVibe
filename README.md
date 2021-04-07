# CqrsVibe
CqrsVibe is an implementation of CQRS with pipelines via GreenPipes

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