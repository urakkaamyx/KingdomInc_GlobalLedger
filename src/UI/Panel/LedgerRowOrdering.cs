using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerRowOrdering
    {
        internal static readonly string[] Fields = { "Tier / Name", "Name", "Inflow", "Outflow", "Net" };
        internal List<LedgerRowData> Arrange(IReadOnlyList<LedgerRowData> rows, string search, int field, bool descending)
            => Level(rows, 0, rows.Count, search.Trim(), field, descending);

        List<LedgerRowData> Level(IReadOnlyList<LedgerRowData> rows, int start, int end, string search, int field, bool descending)
        {
            var blocks = new List<(LedgerRowData Root, List<LedgerRowData> Children, int Original)>();
            int at = start;
            while (at < end)
            {
                var row = rows[at]; int next = at + 1;
                while (next < end && rows[next].Depth > row.Depth) next++;
                bool match = search.Length == 0 || row.Label.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0;
                var children = Level(rows, at + 1, next, match && !row.IsHeader ? "" : search, field, descending);
                if (match && (!row.IsHeader || search.Length == 0) || children.Count != 0) blocks.Add((row, children, at));
                at = next;
            }
            blocks.Sort((a, b) =>
            {
                if (a.Root.IsHeader || b.Root.IsHeader) return a.Original.CompareTo(b.Original);
                int compare;
                if (field >= 2)
                {
                    long av = field == 2 ? a.Root.Income : field == 3 ? a.Root.Outflow : a.Root.Amount;
                    long bv = field == 2 ? b.Root.Income : field == 3 ? b.Root.Outflow : b.Root.Amount;
                    compare = av.CompareTo(bv);
                }
                else if (field == 0 && a.Root.Depth == 0 && a.Root.GameCurrency.HasValue && b.Root.GameCurrency.HasValue)
                    compare = a.Original.CompareTo(b.Original);
                else
                {
                    compare = field == 0 && a.Root.Item != null && b.Root.Item != null ? LedgerUi.ItemTier(a.Root.Item).CompareTo(LedgerUi.ItemTier(b.Root.Item)) : 0;
                    if (compare == 0) compare = StringComparer.CurrentCultureIgnoreCase.Compare(a.Root.Label, b.Root.Label);
                }
                if (compare == 0) compare = a.Original.CompareTo(b.Original);
                return descending ? -compare : compare;
            });
            var result = new List<LedgerRowData>();
            foreach (var block in blocks) { result.Add(block.Root); result.AddRange(block.Children); }
            return result;
        }
    }
}
