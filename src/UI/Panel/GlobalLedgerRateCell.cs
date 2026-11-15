using System.Globalization;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerRateCell
    {
        readonly RectTransform root, coins;
        readonly TMP_Text amount;
        readonly TMP_Text[] coinText = new TMP_Text[3];
        readonly Image[] coinIcon = new Image[3];
        readonly Image icon;
        internal GlobalLedgerRateCell(GlobalLedgerUi ui, Transform parent, string name)
        {
            root = GlobalLedgerUi.Rect(parent, name);
            amount = ui.Text(root, "Amount", "", 16); amount.alignment = TextAlignmentOptions.MidlineRight;
            amount.enableAutoSizing = true; amount.fontSizeMin = 11; amount.fontSizeMax = 16;
            icon = Icon(root, "UnitIcon");
            coins = GlobalLedgerUi.Rect(root, "Coins");
            for (int i = 0; i < 3; i++)
            {
                coinText[i] = ui.Text(coins, "Amount" + i, "", 16); coinText[i].alignment = TextAlignmentOptions.MidlineRight;
                coinIcon[i] = Icon(coins, "Coin" + i);
            }
        }
        static Image Icon(Transform parent, string name)
        {
            var image = GlobalLedgerUi.Rect(parent, name).gameObject.AddComponent<Image>();
            image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        internal void Bind(GlobalLedgerRowData row, decimal value, bool net, GlobalLedgerPanel owner, float x, float width)
        {
            bool visible = !row.IsHeader && (row.GameCurrency.HasValue || row.Item != null);
            root.gameObject.SetActive(visible); if (!visible) return;
            GlobalLedgerUi.Place(root, x, 0, width, 48);
            var color = net ? value > 0m ? GlobalLedgerUi.Gain : value < 0m ? GlobalLedgerUi.Loss : GlobalLedgerUi.Muted : GlobalLedgerUi.Paper;
            bool gold = row.GameCurrency == GameCurrency.Gold;
            coins.gameObject.SetActive(gold); amount.gameObject.SetActive(!gold); icon.gameObject.SetActive(!gold);
            if (gold) { BindGold(value, net, owner, width + 40, color); return; }
            double rate = owner.Rate(value);
            amount.text = (net && rate > 0 ? "+" : "") + rate.ToString("0.##", CultureInfo.InvariantCulture);
            amount.color = color; amount.fontSizeMax = net ? 17 : 16;
            GlobalLedgerUi.Place(amount.rectTransform, 0, 0, width - 24, 48);
            icon.sprite = row.GameCurrency.HasValue ? row.GameCurrency.Value.GetIcon() : row.Item.GetIcon();
            GlobalLedgerUi.Place(icon.rectTransform, width - 18, 15, 18, 18);
        }
        void BindGold(decimal value, bool net, GlobalLedgerPanel owner, float width, Color color)
        {
            decimal scaled = GlobalLedgerMath.RoundWhole(owner.RateDecimal(value));
            var parts = GlobalLedgerUi.SplitGoldCompactDecimal(scaled);
            decimal[] values = { parts.Gold, parts.Silver, parts.Copper };
            Sprite[] sprites = { owner.Design.Asset("Icons_8"), owner.Design.Asset("Icons_9"), owner.Design.Asset("Icons_10") };
            bool[] shown = { parts.Gold != 0m, parts.Silver != 0m || parts.Gold != 0m, parts.Gold == 0m };
            int first = shown[0] ? 0 : shown[1] ? 1 : 2;
            int last = shown[2] ? 2 : 1;
            float total = 0;
            for (int i = 0; i < 3; i++)
            {
                coinText[i].gameObject.SetActive(shown[i]); coinIcon[i].gameObject.SetActive(shown[i]); if (!shown[i]) continue;
                coinText[i].text = (i == first ? scaled < 0m ? "-" : net && scaled > 0m ? "+" : "" : "") + values[i].ToString("#,0", CultureInfo.InvariantCulture);
                coinText[i].color = color; coinText[i].fontSize = net ? 17 : 16;
                coinIcon[i].sprite = sprites[i];
                total += coinText[i].GetPreferredValues().x + 19 + (i == last ? 0 : 6);
            }
            float scale = Mathf.Min(1, (width - 40) / Mathf.Max(1, total));
            GlobalLedgerUi.Place(coins, width - total * scale, 24 * (1 - scale), total, 48);
            coins.localScale = Vector3.one * scale;
            float cursor = 0;
            for (int i = 0; i < 3; i++)
            {
                if (!shown[i]) continue;
                float textWidth = coinText[i].GetPreferredValues().x;
                GlobalLedgerUi.Place(coinText[i].rectTransform, cursor, 0, textWidth, 48);
                GlobalLedgerUi.Place(coinIcon[i].rectTransform, cursor + textWidth + 3, 15, 16, 16);
                cursor += textWidth + 25;
            }
        }
    }
}
