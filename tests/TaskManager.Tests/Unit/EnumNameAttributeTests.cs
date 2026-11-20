using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Tests.Unit;

public sealed class EnumNameAttributeTests
{
    private readonly EnumNameAttribute _attribute = new(typeof(TaskPriority));

    [Theory]
    [InlineData(null)]
    [InlineData("Low")]
    [InlineData("critical")]
    [InlineData("HIGH")]
    public void IsValid_NullOrMemberName_ReturnsTrue(string? value)
    {
        Assert.True(_attribute.IsValid(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Urgent")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("Low, High")]
    [InlineData(" Low")]
    public void IsValid_NotAMemberName_ReturnsFalse(string value)
    {
        Assert.False(_attribute.IsValid(value));
    }

    [Fact]
    public void IsValid_NonString_ReturnsFalse()
    {
        Assert.False(_attribute.IsValid(2));
    }

    [Fact]
    public void FormatErrorMessage_ListsAllowedNames()
    {
        Assert.Equal("Priority must be one of: Low, Medium, High, Critical.", _attribute.FormatErrorMessage("Priority"));
    }

    [Fact]
    public void Parse_IsCaseInsensitive()
    {
        Assert.Equal(TaskPriority.Critical, EnumNameAttribute.Parse<TaskPriority>("cRiTiCaL"));
    }

    [Fact]
    public void Parse_InvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => EnumNameAttribute.Parse<TaskPriority>("Urgent"));
    }

    [Fact]
    public void Constructor_NonEnumType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EnumNameAttribute(typeof(string)));
    }
}
