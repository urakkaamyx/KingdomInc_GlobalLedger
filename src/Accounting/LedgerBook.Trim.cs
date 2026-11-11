using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerBook
    {
        // 30 minutes
               void Trim(long second)
               {
                   foreach (var key in buckets.Keys.TakeWhile(key => key <= second - MaxRetentionSeconds).ToArray()) buckets.Remove(key);
               }

    }
}
