namespace CodingJamAPI.Domain.Common;

/// <summary>
/// Canonical ordering for symmetric pairs (friendships, personal chats): the first id is the smaller one.
/// Compares the canonical lowercase text form, which matches PostgreSQL uuid ordering (unlike
/// Guid.CompareTo, which uses a different byte order and would violate the database check constraints).
/// </summary>
public static class GuidPair
{
    public static (Guid First, Guid Second) Order(Guid a, Guid b) =>
        string.CompareOrdinal(a.ToString(), b.ToString()) <= 0 ? (a, b) : (b, a);
}
