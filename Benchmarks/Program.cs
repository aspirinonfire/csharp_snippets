using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ActivatorHelperBenchmark>();
        }
    }

    [MemoryDiagnoser]
    public class ActivatorHelperBenchmark
    {
        private readonly ServiceProvider _sp = new ServiceCollection()
            .AddSingleton<RegisteredClass1>()
            .BuildServiceProvider();

        private readonly ObjectFactory _cachedFactory =
            ActivatorUtilities.CreateFactory(typeof(CustomClass), new Type[] { typeof(string) });

        [Benchmark(Baseline = true)]
        public void CreateInstanceManually()
        {
            var svc = _sp.GetService<RegisteredClass1>();
            CustomClass instance = new CustomClass(svc, nameof(CreateInstanceManually));
        }

        [Benchmark]
        public void CreateInstance()
        {
            CustomClass instance = ActivatorUtilities.CreateInstance<CustomClass>(_sp, new[] { nameof(CreateInstance) });
        }

        [Benchmark]
        public void CreateFactory()
        {
            var factory = ActivatorUtilities.CreateFactory(typeof(CustomClass), new Type[] { typeof(string) });
            CustomClass instance = factory.Invoke(_sp, new object[] { nameof(CreateFactory) }) as CustomClass;
        }

        [Benchmark]
        public void CreateFactoryGeneric()
        {
            CustomClass instance = CreateGenericFactory<CustomClass>(nameof(CreateFactoryGeneric));
        }

        [Benchmark]
        public void CreateInstanceFromCachedFactory()
        {
            CustomClass instance = _cachedFactory.Invoke(_sp, new object[] { nameof(CreateInstanceFromCachedFactory) }) as CustomClass;
        }

        private T CreateGenericFactory<T>(params object[] runtimeParams) where T: CustomClass
        {
            var factory = ActivatorUtilities.CreateFactory(typeof(T), runtimeParams.Select(p => p.GetType()).ToArray());
            return factory.Invoke(_sp, runtimeParams) as T;
        }
    }

    public class RegisteredClass1
    {
        public string Name { get; } = nameof(RegisteredClass1);
    }

    public class CustomClass
    {
        private readonly RegisteredClass1 _class;
        public string Value { get; }

        public CustomClass(RegisteredClass1 c, string s)
        {
            _class = c;
            Value = s;
        }
    }
}
