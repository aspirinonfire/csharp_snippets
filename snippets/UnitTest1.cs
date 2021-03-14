using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace snippets
{
    public class RegisteredService1
    {
        public string Value { get; }
        public RegisteredService1(string value)
        {
            Value = value;
        }
    }

    public class RegisteredService2
    {
        public string Value => "world";
    }

    public class CustomService
    {
        public string CombinedValue { get; }
        public CustomService(RegisteredService1 service, RegisteredService2 service2, string runtimeValue)
        {
            CombinedValue = $"{service.Value} {service2.Value} {runtimeValue}";
        }
    }

    public class CustomServiceFactory
    {
        private readonly IServiceProvider _sp;
        private readonly ObjectFactory _customSvcDiFactory;

        public CustomServiceFactory(IServiceProvider sp)
        {
            _sp = sp;
            _customSvcDiFactory = ActivatorUtilities.CreateFactory(typeof(CustomService), new Type[] { typeof(string) });
        }

        public CustomService GetCustomService(string runtimeValue)
        {
            object instance = _customSvcDiFactory.Invoke(_sp, new object[] { runtimeValue });
            return (CustomService)instance;
        }
    }

    public class DiFactoryTests
    {
        [Fact]
        public void CanCustomFactory()
        {
            var svcCollection = new ServiceCollection()
                .AddSingleton(sp => new RegisteredService1("hello"))
                .AddSingleton<RegisteredService2>()
                .AddSingleton<CustomServiceFactory>();

            using var sp = svcCollection.BuildServiceProvider();
            var uut = sp.GetService<CustomServiceFactory>();
            var actual = uut.GetCustomService("!!!").CombinedValue;

            Assert.Equal("hello world !!!", actual);
        }
    }
}
