using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerUi
    {
        static readonly string[] TierFirstItems = {
            "iron_ore", "linen_cloth", "conscript", "wood_log", "healing_potion",
            "mercurial_flux", "cattle", "ancient_gem", "soothing_herbs", "widget",
            "prayer_beads", "fragmented_nightmare"
        };
        static Sprite silverCoin, copperCoin;
        internal static readonly Color Paper = new Color(1, .94f, .79f), Muted = new Color(.72f, .67f, .57f),
            Gold = new Color(.94f, .73f, .38f), Gain = new Color(.62f, .85f, .57f), Loss = new Color(.96f, .52f, .44f);
        readonly TMP_Text font;
        readonly Image buttonStyle;
        readonly Sprite frame;

    }
}
