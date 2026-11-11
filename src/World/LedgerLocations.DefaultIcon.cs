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
        static Sprite DefaultIcon(FlowDeco container)
        {
            if (container?.World)
            {
                // Native frames don't expose an icon; use their own background sprite.
                var image = AccessTools.Field(typeof(Assets.Behaviour.Flow.WorldFlowDeco), "_background").GetValue(container.World) as Image;
                if (image && image.sprite) return image.sprite;
            }
            return SpriteLibrary.Get("Icons2_45");
        }

    }
}
