using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly:InternalsVisibleTo("CqrsVibe.Tests")]
[assembly:InternalsVisibleTo("CqrsVibe.MicrosoftDependencyInjection")]

namespace CqrsVibe
{
    /// <summary>
    /// Abstraction of DI container for resolve handlers
    /// </summary>
    public interface IDependencyResolver
    {
        object ResolveService(Type type);

        bool TryResolveService(Type type, out object service);

        IEnumerable<object> ResolveServices(Type type);
    }

    /// <summary>
    /// Accessor of current resolver for current asynchronous control flow
    /// </summary>
    public interface IDependencyResolverAccessor
    {
        IDependencyResolver Current { get; set; }
    }

    internal class DependencyResolverAccessor : IDependencyResolverAccessor
    {
        private static readonly AsyncLocal<IDependencyResolver> CurrentResolver = new AsyncLocal<IDependencyResolver>();

        private readonly IDependencyResolver _rootResolver;

        public DependencyResolverAccessor(IDependencyResolver rootResolverResolver)
        {
            _rootResolver = rootResolverResolver;
        }

        public IDependencyResolver Current
        {
            get => CurrentResolver.Value ?? _rootResolver;
            set => CurrentResolver.Value = value;
        }
    }

    public static class DependencyResolverExtension
    {
        public static TService ResolveService<TService>(this IDependencyResolver resolver) where TService : class
        {
            var service = resolver.ResolveService(typeof(TService)) as TService;
            return service;
        }
    }
}