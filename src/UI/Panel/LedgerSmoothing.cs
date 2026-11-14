using System;
using System.Collections.Generic;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerSmoothing
    {
        readonly Dictionary<string, LedgerSampleWindow> history = new Dictionary<string, LedgerSampleWindow>();
        internal void Clear() => history.Clear();

        internal void Apply(IEnumerable<LedgerRowData> rows, int samples, bool sample)
        {
            foreach (var row in rows)
            {
                if (row.IsHeader || (!row.GameCurrency.HasValue && row.Item == null)) continue;
                if (!history.TryGetValue(row.Key, out var window))
                    history[row.Key] = window = new LedgerSampleWindow();
                window.Apply(row, samples, sample);
            }
        }
    }
}
