using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Items;
using Assets.Source.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerTreeRow
    {
        static TMP_Text CoinText(LedgerUi ui, Transform parent, string name)
        {
            var text = ui.Text(parent, name, "", 18);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

    }
}
