using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("CqrsVibe.Tests")]

namespace CqrsVibe
{
    public interface IHandlerResolver
    {
        object ResolveHandler(Type type);

        IEnumerable<object> ResolveHandlers(Type type);
    }
}