using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerRowData
    {
        internal GlobalLedgerRowData(string key, string label, int depth)
        {
            Key = key;
            Label = label;
            Depth = depth;
        }

        internal readonly string Key, Label;
        internal readonly int Depth;
        internal GameCurrency? GameCurrency;
        internal GameItemType Item;
        internal decimal Amount, Income, Outflow;
        internal float Top;
        internal bool Expandable, Open;
        internal bool IconOnly, IsHeader;
        internal Sprite PrefixIcon, Icon;
        internal GlobalLedgerTarget Target;
    }
}
