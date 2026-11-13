using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerTreeRow
    {
        internal readonly RectTransform Root;
        internal readonly Button Button, ViewButton;
        readonly GlobalLedgerPanel owner;
        readonly TMP_Text label, arrow;
        readonly Image icon, shade, accent, divider;
        readonly GlobalLedgerRowHover hover;
        readonly GlobalLedgerRateCell income, outflow, net;
        readonly int index;
        GlobalLedgerRowData data;

        internal GlobalLedgerTreeRow(GlobalLedgerPanel owner, GlobalLedgerUi ui, Transform parent, int index)
        {
            this.owner = owner; this.index = index;
            Root = GlobalLedgerUi.Rect(parent, "TreeRow" + index);
            shade = ui.Background(Root, Color.clear);
            Button = Root.gameObject.AddComponent<Button>(); Button.targetGraphic = shade;
            Button.transition = Selectable.Transition.None; Button.navigation = new Navigation { mode = Navigation.Mode.None };
            Button.onClick.AddListener(() => { if (data != null) owner.Activate(data); });
            hover = Root.gameObject.AddComponent<GlobalLedgerRowHover>();
            divider = ui.Background(GlobalLedgerUi.Rect(Root, "Divider"), GlobalLedgerDesign.Color("#494140"));
            GlobalLedgerUi.Place(divider.rectTransform, 0, 47, GlobalLedgerDesign.RowWidth, 1); divider.raycastTarget = false;
            accent = ui.Background(GlobalLedgerUi.Rect(Root, "Selection"), GlobalLedgerUi.Gold); accent.raycastTarget = false;
            arrow = ui.Text(Root, "Expander", "", 24); arrow.color = GlobalLedgerUi.Gold; arrow.alignment = TextAlignmentOptions.Center;
            icon = GlobalLedgerUi.Rect(Root, "AssetIcon").gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            label = ui.Text(Root, "Name", "", 16);
            income = new GlobalLedgerRateCell(ui, Root, "In"); outflow = new GlobalLedgerRateCell(ui, Root, "Out"); net = new GlobalLedgerRateCell(ui, Root, "Net");
            ViewButton = ui.Button(Root, "View", "↗", () => { if (data?.Target != null) owner.View(data); }, 16);
            var viewImage = ViewButton.GetComponent<Image>(); viewImage.color = Color.white; viewImage.sprite = owner.Design.Asset("Button03"); viewImage.type = Image.Type.Sliced; viewImage.pixelsPerUnitMultiplier = 1;
            CreateViewIcon(ui);
        }
        void CreateViewIcon(GlobalLedgerUi ui)
        {
            var root = GlobalLedgerUi.Rect(ViewButton.transform, "ViewIcon"); GlobalLedgerUi.Place(root, 16, 5, 16, 16);
            int[,] strokes = { { 0, 5, 7, 1 }, { 0, 5, 1, 10 }, { 0, 14, 11, 1 }, { 10, 9, 1, 6 }, { 8, 1, 7, 1 }, { 14, 1, 1, 7 } };
            for (int i = 0; i < 14; i++)
            {
                var image = ui.Background(GlobalLedgerUi.Rect(root, "Stroke" + i), GlobalLedgerUi.Paper); image.raycastTarget = false;
                if (i < 6) GlobalLedgerUi.Place(image.rectTransform, strokes[i, 0], strokes[i, 1], strokes[i, 2], strokes[i, 3]);
                else GlobalLedgerUi.Place(image.rectTransform, i + 1, 14 - i, 1, 1);
            }
        }
        internal void Bind(GlobalLedgerRowData value, float width)
        {
            data = value; Root.name = value.Key; Root.gameObject.SetActive(true);
            bool selected = owner.SelectedKey == value.Key;
            divider.gameObject.SetActive(!value.IsHeader);
            label.text = value.IconOnly ? "" : value.Label;
            label.color = value.IsHeader || selected ? GlobalLedgerUi.Gold : GlobalLedgerUi.Paper;
            label.fontSize = value.IsHeader ? 12 : 16;
            shade.color = GlobalLedgerDesign.Color(value.IsHeader ? "#343d32" : selected ? "#394437" : index % 2 == 0 ? "#302c30" : "#29262b");
            accent.gameObject.SetActive(selected && !value.IsHeader);
            arrow.text = value.Expandable ? "›" : "";
            arrow.rectTransform.localEulerAngles = value.Open ? new Vector3(0, 0, -90) : Vector3.zero;
            icon.sprite = value.Icon; icon.enabled = value.Icon && !value.IsHeader;
            ViewButton.gameObject.SetActive(value.Target != null && !value.IsHeader); hover.Bind(value.Target);
            income.Bind(value, value.Income, false, owner, GlobalLedgerDesign.IncomeX, 112);
            outflow.Bind(value, value.Outflow, false, owner, GlobalLedgerDesign.OutflowX, 112);
            net.Bind(value, value.Amount, true, owner, GlobalLedgerDesign.NetX, 128);
            Layout(width);
        }
        internal void Layout(float width)
        {
            if (data == null) return;
            float height = data.IsHeader ? 28 : 48;
            GlobalLedgerUi.Place(Root, 0, data.Top, width, height);
            GlobalLedgerUi.Place(accent.rectTransform, 0, 0, 3, height);
            if (data.IsHeader) { GlobalLedgerUi.Place(label.rectTransform, 16, 0, width - 32, height); return; }
            bool basic = owner.Mode == GlobalLedgerPanel.ViewMode.Basic;
            float left = basic ? 16 : 12 + data.Depth * 26;
            GlobalLedgerUi.Place(arrow.rectTransform, left - 5, 10, 24, 28);
            arrow.rectTransform.pivot = new Vector2(.5f, .5f);
            arrow.rectTransform.anchoredPosition = new Vector2(left + 7, -24);
            float iconX = basic ? 16 : left + 26;
            GlobalLedgerUi.Place(icon.rectTransform, iconX, 12, 24, 24);
            float textX = basic ? 52 : data.Depth == 0 ? 72 : 72 + data.Depth * 26;
            // Container/building icons remain visible without shifting the shared amount columns.
            if (data.Depth > 0 && icon.enabled) textX = Mathf.Max(textX, iconX + 32);
            GlobalLedgerUi.Place(label.rectTransform, textX, 0, Mathf.Max(30, GlobalLedgerDesign.IncomeX - textX), height);
            GlobalLedgerUi.Place((RectTransform)ViewButton.transform, GlobalLedgerDesign.ViewX, 11, 48, 26);
        }
    }
}
