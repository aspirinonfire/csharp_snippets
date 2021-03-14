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
    /// |                          Method |          Mean |        Error |       StdDev |    Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
    /// |-------------------------------- |--------------:|-------------:|-------------:|---------:|--------:|-------:|-------:|------:|----------:|
    /// |          CreateInstanceManually |      57.04 ns |     1.172 ns |     1.349 ns |     1.00 |    0.00 | 0.0153 |      - |     - |      32 B |
    /// |                  CreateInstance |   1,039.87 ns |    18.089 ns |    17.766 ns |    18.14 |    0.58 | 0.1030 |      - |     - |     216 B |
    /// |                   CreateFactory | 408,516.88 ns | 5,442.610 ns | 4,824.732 ns | 7,114.47 |  140.52 | 2.9297 | 1.4648 |     - |    6489 B |
    /// |            CreateFactoryGeneric | 410,609.53 ns | 7,565.025 ns | 7,076.329 ns | 7,164.69 |  154.24 | 2.9297 | 1.4648 |     - |    6537 B |
    /// | CreateInstanceFromCachedFactory |      74.80 ns |     1.532 ns |     1.703 ns |     1.31 |    0.05 | 0.0305 |      - |     - |      64 B |
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
