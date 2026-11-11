using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerLocations
    {
        static readonly Regex RichTextTags = new Regex(@"</?[a-zA-Z][^>]*>", RegexOptions.Compiled);
        static Sprite cachedContainerIcon;
        readonly Dictionary<FlowDeco, string> containerIds = new Dictionary<FlowDeco, string>();
        int nextId;

    }
}
