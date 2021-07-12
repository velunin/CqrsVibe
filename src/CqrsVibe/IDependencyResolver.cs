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
        /// <summary>
        /// Get a service
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        object ResolveService(Type type);

        /// <summary>
        /// Try to get a service
        /// </summary>
        /// <param name="type"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        bool TryResolveService(Type type, out object service);

        /// <summary>
        /// Get services
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<object> ResolveServices(Type type);
    }

    /// <summary>
    /// Accessor of current resolver for current asynchronous control flow
    /// </summary>
    public interface IDependencyResolverAccessor
    {
        /// <summary>
        /// Current resolver for current asynchronous control flow
        /// </summary>
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

    /// <summary>
    /// Dependency resolver extensions
    /// </summary>
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Resolve service
        /// </summary>
        /// <param name="resolver"></param>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public static TService ResolveService<TService>(this IDependencyResolver resolver) where TService : class
        {
            var service = resolver.ResolveService(typeof(TService)) as TService;
            return service;
        }
    }
}