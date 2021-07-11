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

            Services.AddValidatorsFromAssembly(GetType().Assembly, ServiceLifetime.Singleton);
            Services.AddSingleton<IDependencyResolver, DependencyResolver>();
            Services.AddSingleton<IDependencyResolverAccessor, DependencyResolverAccessor>();

            Services.AddCqrsVibeHandlers(ServiceLifetime.Singleton, new[] {GetType().Assembly});

            Services.AddSingleton<ReflectionTests.IncorrectMiddlewares.WithoutInvokeMethod>();
            Services.AddSingleton<ReflectionTests.IncorrectMiddlewares.WithWrongInvokeReturnType>();
            Services.AddSingleton<ReflectionTests.IncorrectMiddlewares.WithIncorrectFirstArg>();
            Services.AddSingleton<ReflectionTests.IncorrectMiddlewares.WithIncorrectSecondArg>();

            ResolverAccessor = Get<IDependencyResolverAccessor>();
        }

        public TService Get<TService>()
        {
            return Services.BuildServiceProvider(validateScopes:true).GetService<TService>();
        }
    }
}