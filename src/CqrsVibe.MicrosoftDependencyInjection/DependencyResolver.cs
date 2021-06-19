using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsVibe.MicrosoftDependencyInjection
{
    public class DependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object ResolveService(Type type)
        {
            return _serviceProvider.GetRequiredService(type);
        }

        public IEnumerable<object> ResolveServices(Type type)
        {
            return _serviceProvider.GetServices(type);
        }
    }
}