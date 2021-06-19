using System;
using System.Collections.Generic;

namespace CqrsVibe.Tests
{
    public class DependencyResolver : IDependencyResolver
    {
        private readonly Func<object> _singleHandlerFactory;
        private readonly Func<IEnumerable<object>> _multipleHandlerFactory;
        
        public DependencyResolver(
            Func<object> singleHandlerFactory = null, 
            Func<IEnumerable<object>> multipleHandlerFactory = null)
        {
            _singleHandlerFactory = singleHandlerFactory;
            _multipleHandlerFactory = multipleHandlerFactory;
        }
        
        public object ResolveService(Type type)
        {
            return _singleHandlerFactory?.Invoke();
        }

        public IEnumerable<object> ResolveServices(Type type)
        {
            return _multipleHandlerFactory?.Invoke();
        }
    }
}