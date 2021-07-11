using CqrsVibe.MicrosoftDependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace CqrsVibe.AspNetCore
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseCqrsVibe(this IApplicationBuilder app)
        {
            return app.Use((context, next) =>
            {
                context.RequestServices.SetToHandlerResolverAccessor();
                return next();
            });
        }
    }
}   