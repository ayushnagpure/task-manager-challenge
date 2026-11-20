using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Validation;

/// <summary>
/// Validates that a string is a SQL Server <c>rowversion</c> as returned by the API: base64 that decodes
/// to exactly 8 bytes. Null and empty are treated as valid; combine with <see cref="RequiredAttribute"/>
/// when a value is mandatory (so a missing value reports only "is required").
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class RowVersionAttribute : ValidationAttribute
{
    /// <summary>Length in bytes of a SQL Server <c>rowversion</c>.</summary>
    public const int ByteLength = 8;

    /// <summary>Creates the attribute.</summary>
    public RowVersionAttribute()
        : base("{0} must be the base64 value returned by the API.")
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value switch
        {
            null => true,
            string text when text.Length == 0 => true,
            string text => TryDecode(text, out _),
            _ => false,
        };

    /// <summary>Decodes a rowversion string. Returns false unless it is base64 for exactly 8 bytes.</summary>
    /// <param name="value">The base64 string.</param>
    /// <param name="rowVersion">The decoded bytes, or empty on failure.</param>
    public static bool TryDecode(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // A buffer of exactly 8 bytes: longer values fail to fit, shorter ones are caught by the length check.
        var buffer = new byte[ByteLength];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written != ByteLength)
        {
            return false;
        }

        rowVersion = buffer;
        return true;
    }

    /// <summary>Decodes a rowversion that has already passed validation. Throws if it is not valid.</summary>
    /// <param name="value">The base64 string.</param>
    public static byte[] Decode(string value) =>
        TryDecode(value, out var rowVersion)
            ? rowVersion
            : throw new ArgumentException("Not a valid rowversion.", nameof(value));
}
