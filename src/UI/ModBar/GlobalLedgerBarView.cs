using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerBarView : IModBarView
    {
        internal GlobalLedgerBarView(GlobalLedgerV2Plugin plugin, IModBarContext context)
        {
            this.plugin = plugin;
            button = context.CreateButton("Global Ledger", Toggle);
            StyleButton();
        }

        readonly GlobalLedgerV2Plugin plugin;
        readonly UnityEngine.UI.Button button;
        GlobalLedgerPanel panel;
        readonly GlobalLedgerDesignAssets assets = new GlobalLedgerDesignAssets();
        void StyleButton()
        {
            var image = button.GetComponent<UnityEngine.UI.Image>();
            image.sprite = assets.Get("DecoTitleRed"); image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = .5f; image.color = Color.white;
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            var font = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>().FirstOrDefault(value => value.name == "LiberationSans SDF") ?? TMPro.TMP_Settings.defaultFontAsset;
            if (font) { label.font = font; label.fontSharedMaterial = font.material; }
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 24;
            label.color = GlobalLedgerUi.Paper; label.alignment = TMPro.TextAlignmentOptions.Center;
        }
        public void Dispose()
        {
            if (panel)
            {
                panel.OnPopupClosed -= Refresh;
                if (FoundationApi.UiAccess.Root && panel.gameObject.activeSelf)
                    panel.ClosePopup(false);
                global::UnityEngine.Object.Destroy(panel.gameObject);
            }

            if (plugin.Panel == panel)
                plugin.Panel = null;
            assets.Dispose();
        }

        public void Refresh()
        {
            if (button)
                button.GetComponentInChildren<TMPro.TMP_Text>().text = "Global Ledger";
        }

        void Toggle()
        {
            plugin.EnsurePlayer();
            if (FoundationApi.Kingdom == null || !FoundationApi.UiAccess.Root)
                return;
            if (!panel)
            {
                panel = GlobalLedgerPanel.Create(plugin.Book, button);
                plugin.Panel = panel;
                panel.OnPopupClosed += Refresh;
            }

            panel.TogglePopup();
            Refresh();
        }
    }
}
