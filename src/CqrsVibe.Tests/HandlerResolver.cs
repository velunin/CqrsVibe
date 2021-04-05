using System;
using System.Collections.Generic;

namespace CqrsVibe.Tests
{
    public class HandlerResolver : IHandlerResolver
    {
        private readonly Func<object> _singleHandlerFactory;
        public HandlerResolver(Func<object> singleHandlerFactory)
        {
            _singleHandlerFactory = singleHandlerFactory;
        }
        
        public object ResolveHandler(Type type)
        {
            return _singleHandlerFactory?.Invoke();
        }

        public IEnumerable<object> ResolveHandlers(Type type)
        {
            throw new NotSupportedException();
        }
    }
}