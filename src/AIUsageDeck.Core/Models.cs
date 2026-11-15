namespace AIUsageDeck.Core;

public sealed record QuotaBucket(string GroupId, string GroupName, string Window, double? RemainingPercent, DateTimeOffset? ResetAt);
public sealed record ProviderRead(string Identity, IReadOnlyList<QuotaBucket> Buckets, string Source);
public sealed record ProviderSnapshot(string Provider, string State, string? AccountLabel, IReadOnlyList<QuotaBucket> Buckets,
    DateTimeOffset? ObservedAt, DateTimeOffset? LastAttemptAt, string? ErrorCode, DateTimeOffset? RetryAt, string? Source);
public sealed record UsageSnapshot(int Version, DateTimeOffset GeneratedAt, IReadOnlyList<ProviderSnapshot> Providers);

