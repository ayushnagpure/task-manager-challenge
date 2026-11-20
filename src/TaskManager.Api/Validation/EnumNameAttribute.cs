using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Validation;

/// <summary>
/// Validates that a string is the name of a member of <see cref="EnumType"/> (case-insensitive).
/// Numbers, comma-separated flag combinations and unknown names are rejected. Null is treated as
/// valid; combine with <see cref="RequiredAttribute"/> when a value is mandatory.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class EnumNameAttribute : ValidationAttribute
{
    /// <summary>Creates the attribute for the given enum type.</summary>
    /// <param name="enumType">The enum whose member names are allowed.</param>
    public EnumNameAttribute(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        if (!enumType.IsEnum)
        {
            throw new ArgumentException($"{enumType.Name} is not an enum type.", nameof(enumType));
        }

        EnumType = enumType;
    }

    /// <summary>The enum whose member names are allowed.</summary>
    public Type EnumType { get; }

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value is null || (value is string text && FindName(EnumType, text) is not null);

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        $"{name} must be one of: {string.Join(", ", Enum.GetNames(EnumType))}.";

    /// <summary>
    /// Parses a value that has already passed validation. Throws if the value is not a member name.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">A member name, matched case-insensitively.</param>
    public static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum =>
        FindName(typeof(TEnum), value) is { } name
            ? Enum.Parse<TEnum>(name)
            : throw new ArgumentException($"'{value}' is not a valid {typeof(TEnum).Name}.", nameof(value));

    private static string? FindName(Type enumType, string value) =>
        Enum.GetNames(enumType).FirstOrDefault(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase));
}
