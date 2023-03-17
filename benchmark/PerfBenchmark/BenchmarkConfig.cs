using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;

namespace PerfBenchmark;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default.WithJit(Jit.RyuJit).WithPlatform(Platform.X64).WithRuntime(CoreRuntime.Core80));

        AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, exportDiff: true)));

        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(CsvMeasurementsExporter.Default);
        AddExporter(RPlotExporter.Default);

        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
