using TaskManager.Api.Validation;

namespace TaskManager.Tests.Unit;

public sealed class RowVersionAttributeTests
{
    private static readonly string EightBytes = Convert.ToBase64String([0, 0, 0, 0, 0, 0, 0x07, 0xD1]); // "AAAAAAAAB9E="

    private readonly RowVersionAttribute _attribute = new();

    [Fact]
    public void IsValid_Base64OfEightBytes_ReturnsTrue()
    {
        Assert.True(_attribute.IsValid(EightBytes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_NullOrEmpty_ReturnsTrue_SoRequiredReportsIt(string? value)
    {
        Assert.True(_attribute.IsValid(value));
    }

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("AAAA")]                         // 3 bytes
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")]     // 16 bytes
    [InlineData("AAAAAAAAB9E")]                  // missing padding
    [InlineData(" AAAAAAAAB9E= x")]
    public void IsValid_MalformedOrWrongLength_ReturnsFalse(string value)
    {
        Assert.False(_attribute.IsValid(value));
    }

    [Fact]
    public void IsValid_NonString_ReturnsFalse()
    {
        Assert.False(_attribute.IsValid(new byte[8]));
    }

    [Fact]
    public void FormatErrorMessage_NamesTheField()
    {
        Assert.Equal("RowVersion must be the base64 value returned by the API.", _attribute.FormatErrorMessage("RowVersion"));
    }

    [Fact]
    public void Decode_RoundTripsBytes()
    {
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 0x07, 0xD1 }, RowVersionAttribute.Decode(EightBytes));
    }

    [Fact]
    public void Decode_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => RowVersionAttribute.Decode("AAAA"));
    }
}
