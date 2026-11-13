using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerBarElement : ModBarElement
    {
        internal GlobalLedgerBarElement(GlobalLedgerV2Plugin plugin) : base(GlobalLedgerV2Plugin.Id, "open", 312, 1, ModBarAlignment.Right)
        {
            this.plugin = plugin;
        }

        public override IModBarView CreateView(IModBarContext context) => new GlobalLedgerBarView(plugin, context);
        readonly GlobalLedgerV2Plugin plugin;
    }
}
