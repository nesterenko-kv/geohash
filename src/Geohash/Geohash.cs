using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NetGeohash;

/// <summary>
///     Represents a static class for encoding and decoding geohashes.
///     Geohashes are a hierarchical spatial data structure which subdivides space into buckets of grid shape.
///     This class provides methods to convert geohashes between various formats and to encode/decode latitude
///     and longitude coordinates.
/// </summary>
public static class Geohash
{
    private const int LATITUDE_MIN = -90;
    private const int LATITUDE_MAX = 90;
    private const int LONGITUDE_MIN = -180;
    private const int LONGITUDE_MAX = 180;

    // Alphabet.Length - 1
    private const int CHAR_BITS_MASK = 31;

    // Log2(Alphabet.Length)
    private const int BITS_PER_CHAR = 5;

    private const int BITS_PER_BYTE = 8;

    private const int BITS_PER_HASH = sizeof(ulong) * BITS_PER_BYTE;

    private const int MAX_GEOHASH_PRECISION = BITS_PER_HASH / BITS_PER_CHAR;

    // BITS_PER_CHAR * 1 / BITS_PER_CHAR
    private const int MIN_GEOHASH_PRECISION = 1;

    // double.Exp2(32 bits)
    private const double EXP2_32 = 4294967296D;

    // The "Geohash alphabet" (32ghs) uses all digits 0-9 and almost all lower case
    // letters except "a", "i", "l" and "o".
    private static readonly char[] Base32Text = "0123456789bcdefghjkmnpqrstuvwxyz".ToCharArray();

    //calculated for performance
    //private static byte[] SetupBase32Lookup()
    //{
    //    const int arrSize = byte.MaxValue + 1;
    //    var arr = new byte[arrSize];
    //    for (var i = 0; i < arrSize; i++) arr[i] = byte.MaxValue;
    //    for (var j = 0; j < Base32Text.Length; j++)
    //    {
    //        var index = Base32Text[j];
    //        arr[index] = (byte)j;
    //    }
    //    return arr;
    //}
    private static readonly byte[] CharToBase32 =
    {
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 10, 11, 12, 13, 14, 15, 16, 255, 17, 18, 255, 19, 20, 255, 21, 22, 23, 24, 25, 26,
        27, 28, 29, 30, 31, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255
    };

    private static readonly double[] LatErr = CalculateHalfLatitudePrecisionError();
    private static readonly double[] LngErr = CalculateHalfLongitudePrecisionError();

    private static double[] CalculateHalfLongitudePrecisionError()
    {
        var arr = new double[MAX_GEOHASH_PRECISION + 1];
        arr[0] = LONGITUDE_MAX / 2d;

        for (var precision = 1; precision < MAX_GEOHASH_PRECISION + 1; precision++)
        {
            var precisionBits = BITS_PER_CHAR * precision;

            var lngBits = precisionBits - precisionBits / 2;
            arr[precision] = LONGITUDE_MAX / 2d * double.Exp2(-lngBits);
        }

        return arr;
    }

    private static double[] CalculateHalfLatitudePrecisionError()
    {
        var arr = new double[MAX_GEOHASH_PRECISION + 1];
        arr[0] = LATITUDE_MAX / 2d;

        for (var precision = 1; precision < MAX_GEOHASH_PRECISION + 1; precision++)
        {
            var precisionBits = BITS_PER_CHAR * precision;

            var latBits = precisionBits / 2;
            arr[precision] = LATITUDE_MAX / 2d * double.Exp2(-latBits);
        }

        return arr;
    }

    /// <summary>
    ///     Encodes the given latitude and longitude into a geohash with the specified precision.
    /// </summary>
    /// <param name="latitude">The latitude to encode.</param>
    /// <param name="longitude">The longitude to encode.</param>
    /// <param name="precision">The desired precision of the geohash.</param>
    /// <returns>A ulong representing the encoded geohash.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if <paramref name="latitude" />, <paramref name="longitude" /> or
    ///     <paramref name="precision" /> is invalid.
    /// </exception>
    public static ulong EncodeToInt64(double latitude, double longitude, int precision)
    {
        AssertValidLatitude(latitude);
        AssertValidLongitude(longitude);
        AssertValidGeoHashPrecision(precision);

        return EncodeToInt64Core(latitude, longitude, precision);
    }

    /// <summary>
    ///     Encodes the given geohash string into a ulong.
    /// </summary>
    /// <param name="input">The geohash string to encode.</param>
    /// <returns>A ulong representing the encoded geohash.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the <paramref name="input" /> is <see langword="null" /> or
    ///     <see cref="string.Empty" />, or has invalid precision.
    /// </exception>
    /// <exception cref="FormatException">Thrown if the input string contains invalid characters.</exception>
    public static ulong EncodeToInt64(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        AssertValidGeohashPrecision(input);

        var precision = input.Length;
        var hash = Base32ToInt64(input, precision);

        return hash;
    }

    /// <summary>
    ///     Encodes the given latitude and longitude into a geohash string with the specified precision.
    /// </summary>
    /// <param name="latitude">The latitude to encode.</param>
    /// <param name="longitude">The longitude to encode.</param>
    /// <param name="precision">The desired precision of the geohash.</param>
    /// <returns>A string representing the encoded geohash.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="latitude" />, <paramref name="longitude" /> or
    ///     <paramref name="precision" /> is invalid.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="precision" /> is invalid.</exception>
    public static string Encode(double latitude, double longitude, int precision)
    {
        AssertValidLatitude(latitude);
        AssertValidLongitude(longitude);
        AssertValidGeoHashPrecision(precision);

        var hash = EncodeToInt64Core(latitude, longitude, precision);

        var str = Int64ToBase32(hash, precision);

        return str;
    }

    /// <summary>
    ///     Decodes the given ulong geohash into a geohash string.
    /// </summary>
    /// <param name="input">The ulong geohash to decode.</param>
    /// <returns>A string representing the decoded geohash.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="input" /> geohash has invalid precision.</exception>
    public static string DecodeFromInt64(ulong input)
    {
        var precision = (int) (input & 0xF);
        if (!IsValidGeohashPrecision(precision))
        {
            throw new ArgumentException(
                $"Invalid geohash value \"{input}\". Expected bits precision is between {MIN_GEOHASH_PRECISION * BITS_PER_CHAR} and {MAX_GEOHASH_PRECISION * BITS_PER_CHAR} chars.",
                nameof(input)
            );
        }

        var hash = Int64ToBase32(input, precision);

        return hash;
    }

    /// <summary>
    ///     Decodes a geohash string into its corresponding latitude and longitude coordinates.
    /// </summary>
    /// <param name="input">The geohash string to decode.</param>
    /// <returns>A tuple containing the decoded latitude and longitude coordinates.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the <paramref name="input" /> is <see langword="null" /> or
    ///     <see cref="string.Empty" />, or has invalid precision.
    /// </exception>
    /// <exception cref="FormatException">Thrown if the input string contains invalid characters.</exception>
    public static (double Latitude, double Longitude) Decode(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        AssertValidGeohashPrecision(input);

        var precision = input.Length;
        var hash = Base32ToInt64(input, precision);

        return DecodeFromInt64CoreCentered(hash, precision);
    }

    /// <summary>
    ///     Decodes a geohash string into its corresponding latitude and longitude coordinates.
    /// </summary>
    /// <param name="input">The geohash string to decode.</param>
    /// <returns>A tuple containing the latitude and longitude coordinates of the center of the geohash.</returns>
    /// <param name="precision">The desired precision of the decoded geohash.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if the <paramref name="input" /> is <see langword="null" /> or
    ///     <see cref="string.Empty" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if precision is invalid.</exception>
    /// <exception cref="FormatException">Thrown if the input contains invalid characters for a geohash.</exception>
    public static (double Latitude, double Longitude) Decode(string input, int precision)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        AssertValidGeoHashPrecision(precision);

        if (precision > input.Length)
        {
            throw new ArgumentException(
                $"Invalid geohash precision for string \"{input}\". Ensure precision less or equal to geohash string length.",
                nameof(precision)
            );
        }

        var hash = Base32ToInt64(input, precision);

        return DecodeFromInt64CoreCentered(hash, precision);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (double Latitude, double Longitude) DecodeFromInt64CoreCentered(ulong hash, int precision)
    {
        var (minLatitude, minLongitude) = DecodeFromInt64Core(hash);
        var (latitudeDelta, longitudeDelta) = (LatErr[precision], LngErr[precision]);
        return (minLatitude + latitudeDelta, minLongitude + longitudeDelta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeToInt64Core(double latitude, double longitude, int length)
    {
        Debug.Assert(latitude is >= LATITUDE_MIN and <= LATITUDE_MAX);
        Debug.Assert(longitude is >= LONGITUDE_MIN and <= LONGITUDE_MAX);
        Debug.Assert(length is > 0 and <= MAX_GEOHASH_PRECISION);

        var latitudeRange = EncodeRange(latitude, LATITUDE_MAX);
        var longitudeRange = EncodeRange(longitude, LONGITUDE_MAX);

        var interleaved = Spread(latitudeRange) | (Spread(longitudeRange) << 1);
        var hash = interleaved >> (BITS_PER_HASH - BITS_PER_CHAR * length);
        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (double Latitude, double Longitude) DecodeFromInt64Core(ulong hash)
    {
        // Deinterleave the bits of hash into 32-bit words containing
        // the even and odd bitlevels of hash, respectively.
        var latInt = Squash(hash);
        var lngInt = Squash(hash >> 1);

        var lat = DecodeRange(latInt, LATITUDE_MAX);
        var lng = DecodeRange(lngInt, LONGITUDE_MAX);

        return (lat, lng);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeRange(double x, double r)
    {
        // Encode the position of x within the range -r to +r as a 32-bit integer.
        var p = (x + r) / (2 * r);
        var y = (ulong) (p * EXP2_32);
        return y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double DecodeRange(ulong y, double r)
    {
        // Decode the 32-bit range encoding y back to a value in the range -r to +r.
        var p = y / EXP2_32;
        var x = 2 * r * p - r;
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Spread(ulong x)
    {
        // Spread out the 32 bits of x into 64 bits, where the bits of x occupy even
        // bit positions.
        var k = x;
        k = (k | (k << 16)) & 0x0000ffff0000ffffUL;
        k = (k | (k << 8)) & 0x00ff00ff00ff00ffUL;
        k = (k | (k << 4)) & 0x0f0f0f0f0f0f0f0fUL;
        k = (k | (k << 2)) & 0x3333333333333333UL;
        k = (k | (k << 1)) & 0x5555555555555555UL;
        return k;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Squash(ulong x)
    {
        // Squash the even bitlevels of X into a 32-bit word. Odd bitlevels of X are
        // ignored, and may take any value.
        var k = x & 0x5555555555555555UL;
        k = (k | (k >> 1)) & 0x3333333333333333UL;
        k = (k | (k >> 2)) & 0x0f0f0f0f0f0f0f0fUL;
        k = (k | (k >> 4)) & 0x00ff00ff00ff00ffUL;
        k = (k | (k >> 8)) & 0x0000ffff0000ffffUL;
        k = (k | (k >> 16)) & 0x00000000ffffffffUL;
        return k;
    }

    #region Encode/Decode to Base32

    private static string Int64ToBase32(ulong input, int precision)
    {
        var hash = string.Create(precision, input, Core);

        static void Core(Span<char> buffer, ulong input)
        {
            var hashLength = buffer.Length;

            for (var pos = hashLength - 1; pos >= 0; pos--)
            {
                var charBits = (byte) (input & CHAR_BITS_MASK);
                input >>= BITS_PER_CHAR;
                buffer[pos] = Base32Text[charBits];
            }
        }

        return hash;
    }

    private static ulong Base32ToInt64(ReadOnlySpan<char> input, int precision)
    {
        Debug.Assert(precision is > 0 and <= MAX_GEOHASH_PRECISION);
        Debug.Assert(precision <= input.Length);

        ulong hash = 0;

        foreach (var c in input[..precision])
        {
            hash <<= BITS_PER_CHAR;
            var b = CharToBase32[c];
            if (b == byte.MaxValue)
            {
                throw new FormatException("ObjectId string should only contain hexadecimal characters.");
            }

            hash |= b;
        }

        hash <<= BITS_PER_HASH - precision * BITS_PER_CHAR;
        return hash;
    }

    #endregion

    #region Asserts

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidLatitude(
        double latitude
        )
    {
        return latitude is >= LATITUDE_MIN and <= LATITUDE_MAX;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidLongitude(
        double longitude
        )
    {
        return longitude is >= LONGITUDE_MIN and <= LONGITUDE_MAX;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidGeohashPrecision(
        int precision
        )
    {
        return precision is >= MIN_GEOHASH_PRECISION and <= MAX_GEOHASH_PRECISION;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssertValidLatitude(
        double latitude,
        [CallerArgumentExpression("latitude")] string? paramName = null
        )
    {
        if (!IsValidLatitude(latitude))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                latitude,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Invalid latitude \"{latitude}\". Valid values are between {LATITUDE_MIN} and {LATITUDE_MAX}"
                )
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssertValidLongitude(
        double longitude,
        [CallerArgumentExpression("longitude")] string? paramName = null
        )
    {
        if (!IsValidLongitude(longitude))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                longitude,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Invalid longitude \"{longitude}\". Valid values are between {LONGITUDE_MIN} and {LONGITUDE_MAX}"
                )
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssertValidGeohashPrecision(
        string input,
        [CallerArgumentExpression("input")] string? paramName = null
        )
    {
        if (!IsValidGeohashPrecision(input.Length))
        {
            throw new ArgumentException(
                $"Invalid geohash string \"{input}\". Expected length is between {MIN_GEOHASH_PRECISION} and {MAX_GEOHASH_PRECISION} chars.",
                paramName
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AssertValidGeoHashPrecision(
        int precision,
        [CallerArgumentExpression("precision")] string? paramName = null
        )
    {
        if (!IsValidGeohashPrecision(precision))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                precision,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Invalid precision \"{precision}\". Valid values are between {MIN_GEOHASH_PRECISION} and {MAX_GEOHASH_PRECISION}."
                )
            );
        }
    }

    #endregion
}
