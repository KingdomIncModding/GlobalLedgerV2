using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerLocationEntry
    {
        internal GlobalLedgerBookTotals Totals;
        internal string GroupKey, GroupName, BuildingName;
        internal Sprite GroupIcon, BuildingIcon;
        internal GlobalLedgerTarget GroupTarget, BuildingTarget;
    }
}
