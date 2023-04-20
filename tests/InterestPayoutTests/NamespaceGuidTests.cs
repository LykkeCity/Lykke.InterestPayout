using System;
using InterestPayout.Common.Utils;
using Xunit;

namespace InterestPayoutTests
{
    public class NamespaceGuidTests
    {
        /// <summary> Fixed namespace for tests </summary>
        public static readonly Guid FixedNamespace = new("dc7f3cd2-93e6-4901-b43e-ff01a7a4896c");

        /// <summary>
        /// Natural idempotency id for a single payout "instance" consists of three concatenated values:
        /// <list type="bullet">
        /// <item><description>AssetId (GUID);</description></item>
        /// <item><description>Timestamp of when the recurring message was originally scheduled;</description></item>
        /// <item><description>WalletId (GUID)</description></item>
        /// </list>
        /// Since external services cannot accept such a long idempotency ID for cash in or cash out,
        /// it is needed to create deterministic GUID in a "regular" format, using long idempotencyId as a "seed".
        /// This test checks that using named-based GUIDs functionality with fixed hardcoded namespace is fit to use for this purpose.
        /// </summary>
        [Fact]
        public void CreateVersion5ForLogIdempotencyId()
        {
            const string idempotencyId = "711428df-953c-403e-a0ba-449c92c5b8da:2023-04-20T13:05:02.0000000+00:00:82ec8dce-eb07-4a72-8337-548ac459d7db";
            var guid = NamespaceGuid.Create(
                FixedNamespace,
                idempotencyId,
                version: 5);
            Assert.Equal(new Guid("cb6935b8-cdfb-55e1-a9a8-b81da2d3d31f"), guid);
        }
    }
}
