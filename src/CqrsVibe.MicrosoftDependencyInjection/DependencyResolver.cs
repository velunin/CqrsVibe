using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    /// <summary>
    /// Implementation of <see cref="IDependencyResolver"/> interface
    /// </summary>
    public class DependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initialize a new instance of <see cref="DependencyResolver"/>
        /// </summary>
        /// <param name="serviceProvider"></param>
        public DependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Get a service
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object ResolveService(Type type)
        {
            return _serviceProvider.GetRequiredService(type);
        }

        /// <summary>
        /// Try to get a service
        /// </summary>
        /// <param name="type"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public bool TryResolveService(Type type, out object service)
        {
            service = _serviceProvider.GetService(type);
            return service != null;
        }

        /// <summary>
        /// Get services
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<object> ResolveServices(Type type)
        {
            return _serviceProvider.GetServices(type);
        }
    }
}