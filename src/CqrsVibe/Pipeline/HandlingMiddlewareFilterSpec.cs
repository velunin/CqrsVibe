using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CqrsVibe.ContextAbstractions;
using GreenPipes;

namespace CqrsVibe.Pipeline
{
    internal class HandlingMiddlewareFilterSpec<TContext> : IPipeSpecification<TContext>
        where TContext : class, IHandlingContext
    {
        private readonly Type _middlewareType;

        public HandlingMiddlewareFilterSpec(Type middlewareType)
        {
            _middlewareType = middlewareType ?? throw new ArgumentNullException(nameof(middlewareType));
        }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            builder.AddFilter(new HandlingMiddlewareFilter<TContext>(_middlewareType));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    internal class HandlingMiddlewareFilter<TContext> : IFilter<TContext> where TContext : class, IHandlingContext
    {
        private readonly Func<object, TContext, IPipe<TContext>, IDependencyResolver, Task> _handlingMiddlewareInvoker;

        private object _middleware;
        private readonly Type _middlewareType;

        public HandlingMiddlewareFilter(
            Type middlewareType)
        {
            _middlewareType = middlewareType;
            _handlingMiddlewareInvoker = CompileInvoke(middlewareType);
        }

        public Task Send(TContext context, IPipe<TContext> next)
        {
            if (_middleware == null)
            {
                var rootResolver = context.ContextServices.ResolveService<IDependencyResolver>();
                _middleware = rootResolver.ResolveService(_middlewareType);
            }

            return _handlingMiddlewareInvoker(_middleware, context, next, context.ContextServices);
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("handlingMiddleware");
            scope.Add("middlewareType", _middleware.GetType());
        }

        private static Func<object, TContext, IPipe<TContext>, IDependencyResolver, Task> CompileInvoke(Type middlewareType)
        {
            var contextType = typeof(TContext);
            var nextFilterType = typeof(IPipe<TContext>);

            var invokeMethod = middlewareType.GetMethod(InvokeMethodName, BindingFlags.Instance | BindingFlags.Public);
            if (invokeMethod == null)
            {
                throw new InvalidOperationException(
                    $"No public '{InvokeMethodName}' method found for middleware of type '{middlewareType}'");
            }

            if (invokeMethod.ReturnType != typeof(Task))
            {
                throw new InvalidOperationException(
                    $"'{InvokeMethodName}' does not return an object of type 'Task'");
            }

            var invokeMethodParams = invokeMethod.GetParameters();

            var contextParam = invokeMethodParams[ContextParameterIndex];
            var nextFilterParam = invokeMethodParams[NextFilterParameterIndex];

            if (contextParam.ParameterType != contextType)
            {
                throw new InvalidOperationException(
                    $"The '{InvokeMethodName}' method's first argument must be of type '{contextType}'");
            }

            if (nextFilterParam.ParameterType != nextFilterType)
            {
                throw new InvalidOperationException(
                    $"The '{InvokeMethodName}' method's second argument must be of type '{nextFilterType}'");
            }

            var resolveMethod = typeof(IDependencyResolver).GetMethod(
                nameof(IDependencyResolver.ResolveService), BindingFlags.Instance | BindingFlags.Public);

            var middlewareLambdaParam = Expression.Parameter(typeof(object), "middleware");
            var contextLambdaParam = Expression.Parameter(contextType, contextParam.Name);
            var nextFilterLambdaParam = Expression.Parameter(nextFilterType, nextFilterParam.Name);
            var resolverLambdaParam = Expression.Parameter(typeof(IDependencyResolver), "resolver");

            var invokeParameterExpressions = new List<Expression>(invokeMethodParams.Length)
            {
                contextLambdaParam,
                nextFilterLambdaParam
            };

            for (var i = ParametersForResolveStartsFromIndex; i < invokeMethodParams.Length; i++)
            {
                var param = invokeMethodParams[i];

                invokeParameterExpressions.Add(
                    Expression.Convert(
                        Expression.Call(
                            resolverLambdaParam,
                            resolveMethod!,
                            Expression.Constant(param.ParameterType, typeof(Type))),
                        param.ParameterType));
            }

            var lambda = Expression.Lambda<Func<object, TContext, IPipe<TContext>, IDependencyResolver, Task>>(
                Expression.Call(
                    Expression.Convert(middlewareLambdaParam, middlewareType),
                    invokeMethod!,
                    invokeParameterExpressions),
                middlewareLambdaParam, contextLambdaParam, nextFilterLambdaParam, resolverLambdaParam);

            return lambda.Compile();
        }

        private const int ContextParameterIndex = 0;
        private const int NextFilterParameterIndex = 1;
        private const int ParametersForResolveStartsFromIndex = 2;
        private const string InvokeMethodName = "Invoke";
    }
}