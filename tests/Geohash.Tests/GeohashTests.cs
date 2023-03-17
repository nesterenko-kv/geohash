namespace NetGeohash.Tests;

public class GeohashTests
{
    private const double Tolerance = 1E-07D;

    [TestCase(null)]
    public void Decode_ShouldThrowArgumentException_WhenNullInput(string input)
    {
        Assert.Throws<ArgumentNullException>(() => Geohash.Decode(input));
    }

    [TestCase("abcd", -1)]
    [TestCase("a", 40)]
    [TestCase("5", 13)]
    [TestCase("5ba", 0)]
    public void Decode_ShouldThrowArgumentOutOfRangeException_WhenInvalidPrecision(string input, int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geohash.Decode(input, precision));
    }

    [TestCase("abc", 5)]
    [TestCase("z", 12)]
    public void Decode_ShouldThrowArgumentException_WhenPrecisionGreaterThenInput(string input, int precision)
    {
        Assert.Throws<ArgumentException>(() => Geohash.Decode(input, precision));
    }

    [TestCase("")]
    public void Decode_ShouldThrowArgumentException_WhenEmptyInput(string input)
    {
        Assert.Throws<ArgumentException>(() => Geohash.Decode(input));
    }

    [TestCase(-91.0, 0.0, 1)]
    [TestCase( 91.0, 0.0, 1)]
    public void Encode_ShouldThrowArgumentOutOfRangeException_WhenInvalidLatitude(double latitude, double longitude, int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geohash.Encode(latitude, longitude, precision));
    }

    [TestCase(-181.0, 0.0, 1)]
    [TestCase( 181.0, 0.0, 1)]
    public void Encode_ShouldThrowArgumentOutOfRangeException_WhenInvalidLongitude(double latitude, double longitude, int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geohash.Encode(latitude, longitude, precision));
    }

    [TestCase(0.0, 0.0, 0)]
    [TestCase(0.0, 0.0, 13)]
    public void Encode_ShouldThrowArgumentOutOfRangeException_WhenInvalidPrecision(double latitude, double longitude, int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Geohash.Encode(latitude, longitude, precision));
    }

    [TestCase(-41.2858, 174.7868, 12, "rbsm1k5ug9h6")]
    [TestCase(-12.347856, 34.890273, 3, "kvb")]
    [TestCase(45.678912, 92.452360, 4, "y01e")]
    [TestCase(-89.127865, -179.438962, 7, "000kuyb")]
    [TestCase(80.294617, 19.543821, 5, "uqmbu")]
    public void Encode_ReturnsExpectedResult(double latitude, double longitude, int precision, string expected)
    {
        var result = Geohash.Encode(latitude, longitude, precision);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("rbsm1k5ug9h6", 12, -41.2857999, 174.7867999)]
    [TestCase("9q8yy9mf", 8, 37.7562761, -122.4016857)]
    public void Decode_ReturnsExpectedResult(string geohash, int precision, double expectedLatitude, double expectedLongitude)
    {
        var (latitude, longitude) = Geohash.Decode(geohash, precision);

        Assert.Multiple(
            () =>
            {
                Assert.That(latitude, Is.EqualTo(expectedLatitude).Within(Tolerance));
                Assert.That(longitude, Is.EqualTo(expectedLongitude).Within(Tolerance));
            }
        );
    }
}
