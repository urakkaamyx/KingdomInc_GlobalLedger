using System;
using System.Collections.Generic;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerSampleWindow
    {
        readonly Queue<(long Income, long Outflow)> values = new Queue<(long, long)>();
        decimal income, outflow;

        internal void Apply(LedgerRowData row, int count, bool sample)
        {
            if (sample || values.Count == 0)
            {
                values.Enqueue((row.Income, row.Outflow));
                income += row.Income;
                outflow += row.Outflow;
            }
            while (values.Count > Math.Max(1, count))
            {
                var previous = values.Dequeue();
                income -= previous.Income;
                outflow -= previous.Outflow;
            }
            row.Income = (long)Math.Round(income / values.Count, MidpointRounding.AwayFromZero);
            row.Outflow = (long)Math.Round(outflow / values.Count, MidpointRounding.AwayFromZero);
            row.Amount = row.Income - row.Outflow;
        }
    }
}
