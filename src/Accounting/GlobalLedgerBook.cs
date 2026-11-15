using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerBook
    {
        sealed class Bucket
        {
            internal long MinSecond = long.MaxValue;
            internal long MaxSecond = long.MinValue;
            internal readonly Dictionary<string, GlobalLedgerBookTotals> Entries = new Dictionary<string, GlobalLedgerBookTotals>(StringComparer.Ordinal);

            internal void IncludeSecond(long second)
            {
                if (second < MinSecond) MinSecond = second;
                if (second > MaxSecond) MaxSecond = second;
            }

            internal void IncludeRange(long minSecond, long maxSecond)
            {
                if (minSecond < MinSecond) MinSecond = minSecond;
                if (maxSecond > MaxSecond) MaxSecond = maxSecond;
            }
        }

        sealed class BucketTier
        {
            internal readonly int SpanSeconds;
            internal readonly int HorizonSeconds;
            internal readonly SortedDictionary<long, Bucket> Buckets = new SortedDictionary<long, Bucket>();
            internal readonly List<long> ScratchExpired = new List<long>();

            internal BucketTier(int spanSeconds, int horizonSeconds)
            {
                SpanSeconds = spanSeconds;
                HorizonSeconds = horizonSeconds;
            }
        }

        internal GlobalLedgerBook(Func<double> clock)
        {
            this.clock = clock;
            tiers = new[]
            {
                new BucketTier(1, 60),
                new BucketTier(10, 1800),
                new BucketTier(60, 7200),
                new BucketTier(600, MaxRetentionSeconds)
            };
            Reset();
        }

        internal const int CurrencyCount = 9;
        internal const int MaxRetentionSeconds = 24 * 60 * 60;
        readonly Func<double> clock;
        readonly BucketTier[] tiers;
        readonly Dictionary<string, double> lastItemActivity = new Dictionary<string, double>();
        readonly Dictionary<int, double> lastCurrencyActivity = new Dictionary<int, double>();
        double started;
        long lastTrim = long.MinValue;
        internal long BatchWrites { get; private set; }
        internal double Elapsed => Math.Max(0, clock() - started);

        internal int RetainedBucketCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < tiers.Length; i++) count += tiers[i].Buckets.Count;
                return count;
            }
        }

        internal int RetainedRecordCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < tiers.Length; i++)
                    foreach (var pair in tiers[i].Buckets)
                        count += pair.Value.Entries.Count;
                return count;
            }
        }

        internal int RetainedAllocationCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < tiers.Length; i++)
                    foreach (var pair in tiers[i].Buckets)
                        foreach (var record in pair.Value.Entries.Values)
                            count += record.AllocationCount;
                return count;
            }
        }

        internal bool HasCurrencyActivity(int currency) => lastCurrencyActivity.ContainsKey(currency);
        internal bool HasItemActivity(string itemId) => !string.IsNullOrEmpty(itemId) && lastItemActivity.ContainsKey(itemId);

        /// <summary>Commit one pooled operation batch with one clock read and bucket lookup.</summary>
        internal bool RecordBatch(GlobalLedgerFrame frame)
        {
            bool active = frame.Produced.Count != 0 || frame.Consumed.Count != 0;
            for (int c = 0; c < CurrencyCount; c++)
                active |= frame.Income[c] > 0m || frame.Spending[c] > 0m;
            if (!active)
                return false;
            var source = GlobalLedgerObserver.Describe(frame.Element);
            double time = clock();
            long second = (long)Math.Floor(time);
            Trim(second);
            var totals = EntryFor(second, second, source);
            for (int c = 0; c < CurrencyCount; c++)
            {
                decimal earned = Math.Max(0m, frame.Income[c]);
                decimal spent = Math.Max(0m, frame.Spending[c]);
                if (earned == 0m && spent == 0m)
                    continue;
                lastCurrencyActivity[c] = time;
                totals.AddEarned(c, earned);
                totals.AddSpent(c, spent);
            }
            foreach (var pair in frame.Produced)
            {
                if (pair.Value == 0m) continue;
                totals.AddItemProduced(pair.Key, pair.Value);
                lastItemActivity[pair.Key] = time;
            }
            foreach (var pair in frame.Consumed)
            {
                if (pair.Value == 0m) continue;
                totals.AddItemConsumed(pair.Key, pair.Value);
                lastItemActivity[pair.Key] = time;
            }
            BatchWrites++;
            return true;
        }

        internal void Record(GlobalLedgerBookSource source, GameCurrency currency, decimal earned, decimal spent)
        {
            if (earned == 0m && spent == 0m)
                return;
            double time = clock();
            lastCurrencyActivity[(int)currency] = time;
            long second = (long)Math.Floor(time);
            Trim(second);
            var totals = EntryFor(second, second, source);
            totals.AddEarned((int)currency, earned);
            totals.AddSpent((int)currency, spent);
        }

        internal void RecordItem(GlobalLedgerBookSource source, string itemId, decimal produced, decimal consumed)
        {
            if ((produced == 0m && consumed == 0m) || string.IsNullOrEmpty(itemId))
                return;
            double time = clock();
            lastItemActivity[itemId] = time;
            long second = (long)Math.Floor(time);
            Trim(second);
            var totals = EntryFor(second, second, source);
            totals.AddItemProduced(itemId, produced);
            totals.AddItemConsumed(itemId, consumed);
        }

        internal void Reset()
        {
            for (int i = 0; i < tiers.Length; i++) tiers[i].Buckets.Clear();
            lastItemActivity.Clear();
            lastCurrencyActivity.Clear();
            lastTrim = long.MinValue;
            BatchWrites = 0;
            started = clock();
        }

        internal IReadOnlyList<GlobalLedgerBookTotals> Snapshot(int windowSeconds = 60)
        {
            if (windowSeconds <= 0)
                windowSeconds = 1;
            long now = (long)Math.Floor(clock());
            long winStart = now - windowSeconds + 1;
            Trim(now);
            var result = new Dictionary<string, GlobalLedgerBookTotals>(StringComparer.Ordinal);
            for (int i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                if (tier.Buckets.Count == 0) continue;
                foreach (var pairBucket in tier.Buckets)
                {
                    var bucket = pairBucket.Value;
                    if (bucket.MaxSecond < winStart) continue;
                    if (bucket.MinSecond > now)
                    {
                        if (pairBucket.Key > now) break;
                        continue;
                    }
                    long activeSpan = bucket.MaxSecond - bucket.MinSecond + 1;
                    long overlapStart = bucket.MinSecond > winStart ? bucket.MinSecond : winStart;
                    long overlapEnd = bucket.MaxSecond < now ? bucket.MaxSecond : now;
                    long overlap = overlapEnd - overlapStart + 1;
                    if (overlap <= 0) continue;
                    foreach (var pair in bucket.Entries)
                    {
                        if (!result.TryGetValue(pair.Key, out var totals))
                            result[pair.Key] = totals = new GlobalLedgerBookTotals(pair.Value.Source);
                        totals.MergeFrom(pair.Value, overlap, activeSpan);
                    }
                }
            }
            return result.Values.ToArray();
        }

        internal double TimeSinceCurrencyActivity(int currency) => lastCurrencyActivity.TryGetValue(currency, out var time) ? Math.Max(0, clock() - time) : double.MaxValue;
        internal double TimeSinceItemActivity(string itemId) => !string.IsNullOrEmpty(itemId) && lastItemActivity.TryGetValue(itemId, out var time) ? Math.Max(0, clock() - time) : double.MaxValue;

        GlobalLedgerBookTotals EntryFor(long second, long now, GlobalLedgerBookSource source)
        {
            long age = Math.Max(0, now - second);
            var tier = tiers[tiers.Length - 1];
            for (int i = 0; i < tiers.Length; i++)
            {
                if (age < tiers[i].HorizonSeconds)
                {
                    tier = tiers[i];
                    break;
                }
            }
            long bucketStart = AlignFloor(second, tier.SpanSeconds);
            var bucket = GetOrCreateBucket(tier, bucketStart);
            bucket.IncludeSecond(second);
            return GetOrCreateEntry(bucket, source);
        }

        static Bucket GetOrCreateBucket(BucketTier tier, long bucketStart)
        {
            if (!tier.Buckets.TryGetValue(bucketStart, out var bucket))
                tier.Buckets[bucketStart] = bucket = new Bucket();
            return bucket;
        }

        static GlobalLedgerBookTotals GetOrCreateEntry(Bucket bucket, GlobalLedgerBookSource source)
        {
            if (!bucket.Entries.TryGetValue(source.Id, out var totals))
                bucket.Entries[source.Id] = totals = new GlobalLedgerBookTotals(source);
            totals.Source = source;
            return totals;
        }

        static long AlignFloor(long second, int span)
        {
            if (span <= 1) return second;
            long quotient = second / span;
            if (second < 0 && second % span != 0) quotient--;
            return quotient * span;
        }

        // Retain the longest selectable window (24 hours) via tiered buckets.
        void Trim(long now)
        {
            if (now <= lastTrim)
                return;
            lastTrim = now;
            for (int i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                if (tier.Buckets.Count == 0) continue;
                long minEndToKeep = now - tier.HorizonSeconds + 1;
                tier.ScratchExpired.Clear();
                foreach (var pair in tier.Buckets)
                {
                    if (pair.Value.MaxSecond < minEndToKeep)
                        tier.ScratchExpired.Add(pair.Key);
                    else if (pair.Key + tier.SpanSeconds - 1 >= minEndToKeep)
                        break;
                }
                if (tier.ScratchExpired.Count == 0) continue;
                var nextTier = i + 1 < tiers.Length ? tiers[i + 1] : null;
                long retentionFloor = now - MaxRetentionSeconds + 1;
                for (int j = 0; j < tier.ScratchExpired.Count; j++)
                {
                    long expiredKey = tier.ScratchExpired[j];
                    var bucket = tier.Buckets[expiredKey];
                    tier.Buckets.Remove(expiredKey);
                    if (nextTier == null || bucket.MaxSecond < retentionFloor)
                        continue;
                    long targetKey = AlignFloor(expiredKey, nextTier.SpanSeconds);
                    var targetBucket = GetOrCreateBucket(nextTier, targetKey);
                    targetBucket.IncludeRange(bucket.MinSecond, bucket.MaxSecond);
                    foreach (var entry in bucket.Entries.Values)
                        GetOrCreateEntry(targetBucket, entry.Source).MergeFrom(entry);
                }
            }
        }
    }
}
