using GeoHash.NetCore.Utilities.Decoders;
using NetGeohash;
using NGeoHash;

namespace PerfBenchmark.Suite;

[Config(typeof(BenchmarkConfig))]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Decode
{
    private GeoHashDecoder<string> _decoder;

    [ParamsSource(nameof(ValuesForInput))]
    public string Input { get; set; }

    public IEnumerable<string> ValuesForInput => new[]
    {
        "k",
        "yd",
        "6n3",
        "zvgk",
        "t05kh",
        "b5cv2h",
        "vveyj80",
        "f7y53xjt",
        "trm92jkbv",
        "drmq3gx6zt",
        "9zefgnuj7dw",
        "k9m2h7t1n0c2"
    };

    [Benchmark]
    public (double Latitude, double Longitude) NetGeohash()
    {
        return Geohash.Decode(Input);
    }

    [GlobalSetup(Target = nameof(NetCoreGeohash))]
    public void NetCoreGeohashGlobalSetup()
    {
        _decoder = new GeoHashDecoder<string>();
    }

    [Benchmark]
    public Tuple<double, double> NetCoreGeohash()
    {
        return _decoder.DecodeAsTuple(Input);
    }

    [Benchmark]
    public GeohashDecodeResult NGeoHashLib()
    {
        return NGeoHash.GeoHash.Decode(Input);
    }
}
