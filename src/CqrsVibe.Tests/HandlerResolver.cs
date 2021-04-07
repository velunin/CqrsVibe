using System;
using System.Collections.Generic;

namespace CqrsVibe.Tests
{
    public class HandlerResolver : IHandlerResolver
    {
        private readonly Func<object> _singleHandlerFactory;
        private readonly Func<IEnumerable<object>> _multipleHandlerFactory;
        
        public HandlerResolver(
            Func<object> singleHandlerFactory = null, 
            Func<IEnumerable<object>> multipleHandlerFactory = null)
        {
            _singleHandlerFactory = singleHandlerFactory;
            _multipleHandlerFactory = multipleHandlerFactory;
        }
        
        public object ResolveHandler(Type type)
        {
            return _singleHandlerFactory?.Invoke();
        }

        public IEnumerable<object> ResolveHandlers(Type type)
        {
            return _multipleHandlerFactory?.Invoke();
        }
    }
}