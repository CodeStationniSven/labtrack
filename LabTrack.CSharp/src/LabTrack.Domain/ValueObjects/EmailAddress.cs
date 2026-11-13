namespace LabTrack.Domain.ValueObjects;

public class EmailAddress : IEquatable<EmailAddress>
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email address cannot be empty.", nameof(value));
        }

        var trimmedValue = value.Trim();

        if (!IsValidEmail(trimmedValue))
        {
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));
        }

        Value = trimmedValue.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(EmailAddress? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as EmailAddress);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString() => Value;

    public static bool operator ==(EmailAddress? left, EmailAddress? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(EmailAddress? left, EmailAddress? right)
    {
        return !(left == right);
    }

    public static implicit operator string(EmailAddress email) => email.Value;
}