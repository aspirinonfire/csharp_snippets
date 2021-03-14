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

    /// <summary>
    /// |                          Method |         Mean |       Error |      StdDev |  Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
    /// |-------------------------------- |-------------:|------------:|------------:|-------:|--------:|-------:|-------:|------:|----------:|
    /// |                  CreateInstance |   1,015.2 ns |    19.37 ns |    21.53 ns |   1.00 |    0.00 | 0.1297 |      - |     - |     272 B |
    /// |                   CreateFactory | 406,110.2 ns | 7,775.55 ns | 6,892.82 ns | 401.82 |    9.19 | 2.9297 | 1.4648 |     - |    6545 B |
    /// |            CreateFactoryGeneric | 404,532.1 ns | 7,827.73 ns | 8,375.59 ns | 398.65 |   11.74 | 2.9297 | 1.4648 |     - |    6601 B |
    /// | CreateInstanceFromCachedFactory |     114.5 ns |     2.29 ns |     4.97 ns |   0.11 |    0.01 | 0.0725 |      - |     - |     152 B |
    /// </summary>
    [MemoryDiagnoser]
    public class ActivatorHelperBenchmark
    {
        private readonly ServiceProvider _sp = new ServiceCollection()
            .AddSingleton<RegisteredClass1>()
            .BuildServiceProvider();

        private readonly ObjectFactory _cachedFactory =
            ActivatorUtilities.CreateFactory(typeof(CustomClass), new Type[] { typeof(string) });

        [Benchmark(Baseline = true)]
        public void CreateInstance()
        {
            CustomClass instance = ActivatorUtilities.CreateInstance<CustomClass>(_sp, new[] { nameof(CreateInstance) });
            string c = "a" + instance.Value;
        }

        [Benchmark]
        public void CreateFactory()
        {
            var factory = ActivatorUtilities.CreateFactory(typeof(CustomClass), new Type[] { typeof(string) });
            CustomClass instance = factory.Invoke(_sp, new object[] { nameof(CreateFactory) }) as CustomClass;
            string c = "a" + instance.Value;
        }

        [Benchmark]
        public void CreateFactoryGeneric()
        {
            CustomClass instance = CreateGenericFactory<CustomClass>(nameof(CreateFactoryGeneric));
            string c = "a" + instance.Value;
        }

        [Benchmark]
        public void CreateInstanceFromCachedFactory()
        {
            CustomClass instance = _cachedFactory.Invoke(_sp, new object[] { nameof(CreateInstanceFromCachedFactory) }) as CustomClass;
            string c = "a" + instance.Value;
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
