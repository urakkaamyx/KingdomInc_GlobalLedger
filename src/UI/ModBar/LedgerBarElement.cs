using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerBarElement : ModBarElement
    {
        internal LedgerBarElement(GlobalLedgerPlugin plugin) : base(GlobalLedgerPlugin.Id, "open", 220, 0, ModBarAlignment.Right)
        {
            this.plugin = plugin;
        }

        public override IModBarView CreateView(IModBarContext context) => new LedgerBarView(plugin, context);
        readonly GlobalLedgerPlugin plugin;
    }
}
