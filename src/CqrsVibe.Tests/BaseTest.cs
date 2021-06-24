using CqrsVibe.MicrosoftDependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CqrsVibe.Tests
{
    public class BaseTest
    {
        protected IServiceCollection Services;
            
        protected IDependencyResolverAccessor ResolverAccessor;
           
        [OneTimeSetUp]
        public void SetUp()
        {
            Services = new ServiceCollection();

            Services.AddValidatorsFromAssembly(GetType().Assembly);
            Services.AddSingleton<IDependencyResolver, DependencyResolver>();
            Services.AddSingleton<IDependencyResolverAccessor, DependencyResolverAccessor>();

            Services.AddCqrsVibeHandlers(ServiceLifetime.Singleton, new[] {GetType().Assembly});

            ResolverAccessor = Get<IDependencyResolverAccessor>();
        }

        public TService Get<TService>()
        {
            return Services.BuildServiceProvider().GetService<TService>();
        }
    }
}