using BenchmarkDotNet.Running;

namespace MyPackage.Benchmarks;

internal static class Program
{
    // Run through `build.ps1 RunBenchmarks`, or directly using `dotnet run -c Release -- --filter *`
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
