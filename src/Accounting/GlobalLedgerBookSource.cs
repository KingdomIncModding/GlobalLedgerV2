using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerBookSource
    {
        internal GlobalLedgerBookSource(
            string id,
            string typeId,
            string typeName,
            string name,
            string locale = null,
            string rawDefaultLabel = null,
            string rawDetailKey = null,
            string rawFlowLabel = null,
            Vector2Int position = default)
        {
            Id = id;
            TypeId = typeId;
            TypeName = typeName;
            Name = name;
            Locale = locale;
            RawDefaultLabel = rawDefaultLabel;
            RawDetailKey = rawDetailKey;
            RawFlowLabel = rawFlowLabel;
            Position = position;
        }

        internal readonly string Id, TypeId, TypeName, Name;
        internal readonly string Locale, RawDefaultLabel, RawDetailKey, RawFlowLabel;
        internal readonly Vector2Int Position;

        internal string LocalizedName(string currentLocale)
        {
            if (Locale == currentLocale || (RawDetailKey == null && RawFlowLabel == null && RawDefaultLabel == null))
                return Name;
            if (TypeId == "War")
            {
                string warName = FoundationApi.GameInfo.Translate(RawDefaultLabel);
                return string.IsNullOrEmpty(warName) || warName.StartsWith("@") ? Name : warName;
            }
            string detail = FoundationApi.GameInfo.Translate(RawDetailKey ?? "");
            return (GlobalLedgerLocations.CleanLabel(RawFlowLabel) ?? "Workspace") + " / " + detail + " [" + Position.x + ", " + Position.y + "]";
        }
    }
}
