using GeoHash.NetCore.Enums;
using GeoHash.NetCore.Utilities.Encoders;
using NetGeohash;

namespace PerfBenchmark.Suite;

[Config(typeof(BenchmarkConfig))]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "No static in benchmark")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Encode
{
    private GeoHashEncoder<string> _encoder;

    public IEnumerable<object[]> Values()
    {
        yield return new object[] {-12.347856, 34.890273, 3};
        yield return new object[] {45.678912, 92.452360, 4};
        yield return new object[] {80.294617, 19.543821, 5};
        yield return new object[] {-89.127865, -179.438962, 7};
        yield return new object[] {52.5174, 13.409, 12};
        yield return new object[] {-41.2858, 174.7868, 12};
    }

    [Benchmark]
    [ArgumentsSource(nameof(Values))]
    public string NetGeohash(double latitude, double longitude, int precision)
    {
        return Geohash.Encode(latitude, longitude, precision);
    }

    [GlobalSetup(Target = nameof(NetCoreGeohash))]
    public void NetCoreGeohashGlobalSetup()
    {
        _encoder = new GeoHashEncoder<string>();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Values))]
    public string NetCoreGeohash(double latitude, double longitude, int precision)
    {
        return _encoder.Encode(latitude, longitude, (GeoHashPrecision) precision);
    }

    [Benchmark]
    [ArgumentsSource(nameof(Values))]
    public string NGeoHashLib(double latitude, double longitude, int precision)
    {
        return NGeoHash.GeoHash.Encode(latitude, longitude, precision);
    }
}
