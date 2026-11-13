namespace LabTrack.Domain.ValueObjects;

public class DateRange : IEquatable<DateRange>
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeSpan Duration => End - Start;

    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ArgumentException("End date cannot be earlier than start date.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public bool Overlaps(DateRange other)
    {
        return Start < other.End && End > other.Start;
    }

    public bool Contains(DateTime dateTime)
    {
        return dateTime >= Start && dateTime <= End;
    }

    public bool Equals(DateRange? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Start == other.Start && End == other.End;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DateRange);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }

    public static bool operator ==(DateRange? left, DateRange? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(DateRange? left, DateRange? right)
    {
        return !(left == right);
    }
}