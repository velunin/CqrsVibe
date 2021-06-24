using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly:InternalsVisibleTo("CqrsVibe.Tests")]
[assembly:InternalsVisibleTo("CqrsVibe.MicrosoftDependencyInjection")]

namespace CqrsVibe
{
    public interface IDependencyResolver
    {
        object ResolveService(Type type);

        bool TryResolveService(Type type, out object service);

        IEnumerable<object> ResolveServices(Type type);
    }

    public interface IDependencyResolverAccessor
    {
        IDependencyResolver Current { get; set; }
    }

    internal class DependencyResolverAccessor : IDependencyResolverAccessor
    {
        private static readonly AsyncLocal<IDependencyResolver> CurrentResolver = new AsyncLocal<IDependencyResolver>();

        private readonly IDependencyResolver _defaultResolver;

        public DependencyResolverAccessor(IDependencyResolver defaultResolver)
        {
            _defaultResolver = defaultResolver;
        }

        public IDependencyResolver Current
        {
            get => CurrentResolver.Value ?? _defaultResolver;
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