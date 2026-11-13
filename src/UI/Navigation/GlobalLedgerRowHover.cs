using KingdomFoundation.Api;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal void Bind(GlobalLedgerTarget value)
        {
            if (inside && !GlobalLedgerTarget.Same(target, value))
                GlobalLedgerV2Plugin.Instance?.Navigation?.Unhover(target);
            target = value;
            if (inside && target != null)
                GlobalLedgerV2Plugin.Instance?.Navigation?.Hover(target);
        }

        GlobalLedgerTarget target;
        bool inside;
        void OnDisable()
        {
            if (inside)
                OnPointerExit(null);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            inside = true;
            if (target != null)
                GlobalLedgerV2Plugin.Instance?.Navigation?.Hover(target);
        }

        public void OnPointerExit(PointerEventData data)
        {
            inside = false;
            GlobalLedgerV2Plugin.Instance?.Navigation?.Unhover(target);
        }
    }
}
