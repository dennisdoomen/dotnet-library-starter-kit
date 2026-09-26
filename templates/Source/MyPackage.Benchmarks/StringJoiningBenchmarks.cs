using System.Text;
using BenchmarkDotNet.Attributes;

namespace MyPackage.Benchmarks;

/// <summary>
/// An example benchmark that shows the basic setup. Replace it with benchmarks that exercise the hot paths of your library.
/// </summary>
[MemoryDiagnoser]
public class StringJoiningBenchmarks
{
    private string[] values = [];

    [Params(10, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        values = new string[Count];
        for (int index = 0; index < Count; index++)
        {
            values[index] = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    [Benchmark(Baseline = true)]
    public string StringJoin() => string.Join(",", values);

    [Benchmark]
    public string StringBuilder()
    {
        var builder = new StringBuilder();
        foreach (string value in values)
        {
            if (builder.Length > 0)
            {
                builder.Append(',');
            }

            builder.Append(value);
        }

        return builder.ToString();
    }
}
