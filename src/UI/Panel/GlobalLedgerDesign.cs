using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    // Native designer layout, narrowed to fit compact Sort and Display controls.
    internal sealed class GlobalLedgerDesign : IDisposable
    {
        internal const float Width = 816, Height = 856, RowWidth = 736;
        internal const float ControlsHeight = 120;
        internal const float IncomeX = 254, OutflowX = 378, NetX = 502, ViewX = 680;
        readonly Dictionary<string, RectTransform> nodes = new Dictionary<string, RectTransform>();
        readonly GlobalLedgerUi ui;
        readonly RectTransform root;
        readonly GlobalLedgerDesignAssets assets = new GlobalLedgerDesignAssets();
        RectTransform menu, overviewArrow;
        internal GlobalLedgerDesign(GlobalLedgerUi ui, RectTransform root)
        {
            this.ui = ui; this.root = root;
            Group("01 \u00b7 Window frame", "", 0.0f, 0.0f, 816.0f, 856.0f);
            Sprite("01 \u00b7 Window frame/Main panel", "01 \u00b7 Window frame", 0.0f, 0.0f, 816.0f, 856.0f, "DecoRectDark", true, "#30282f", "#a29da7", 1.0f);
            Group("02 \u00b7 Header controls", "", 24.0f, 24.0f, 768.0f, 196.0f);
            Sprite("02 \u00b7 Header controls/Header background", "02 \u00b7 Header controls", 0.0f, 0.0f, 768.0f, 196.0f, "DecoRectDark", true, "#344237", "#7b816b", 1.0f);
            Text("02 \u00b7 Header controls/Overview heading", "02 \u00b7 Header controls", 20.0f, 10.0f, 476.0f, 36.0f, "KINGDOM OVERVIEW", 22.0f, "#e2bd77", "left");
            Text("02 \u00b7 Header controls/Overview subtitle", "02 \u00b7 Header controls", 20.0f, 46.0f, 476.0f, 24.0f, "Economy and production across your kingdom", 13.0f, "#a7aa95", "left");
            Block("02 \u00b7 Header controls/Live status dot", "02 \u00b7 Header controls", 532.0f, 26.0f, 8.0f, 8.0f, "#acd48c", 1.0f);
            Text("02 \u00b7 Header controls/Live update status", "02 \u00b7 Header controls", 554.0f, 15.0f, 190.0f, 28.0f, "LIVE  \u00b7  Refresh 1s", 12.0f, "#acd48c", "left");
            Group("02 \u00b7 Header controls/Rate", "02 \u00b7 Header controls", 16.0f, 82.0f, 248.0f, 110.0f);
            Sprite("02 \u00b7 Header controls/Rate/Control background", "02 \u00b7 Header controls/Rate", 0.0f, 0.0f, 248.0f, 110.0f, "DecoRectDark", true, "#2d382f", "#9e875b", 1.0f);
            Group("02 \u00b7 Header controls/Rate/Control heading", "02 \u00b7 Header controls/Rate", 16.0f, -10.0f, 100.0f, 30.0f);
            Sprite("02 \u00b7 Header controls/Rate/Control heading/Gold title plaque", "02 \u00b7 Header controls/Rate/Control heading", 0.0f, 0.0f, 100.0f, 30.0f, "DecoTitleYellow", true, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Rate/Control heading/Section label", "02 \u00b7 Header controls/Rate/Control heading", 10.0f, 0.0f, 80.0f, 30.0f, "Rate", 14.0f, "#302718", "center");
            Group("02 \u00b7 Header controls/Rate/Rate window 1", "02 \u00b7 Header controls/Rate", 16.0f, 24.0f, 70.0f, 28.0f);
            Sprite("02 \u00b7 Header controls/Rate/Rate window 1/Radio unselected", "02 \u00b7 Header controls/Rate/Rate window 1", 0.0f, 4.0f, 20.0f, 20.0f, "ResizeHandles_1", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Rate/Rate window 1/Option label", "02 \u00b7 Header controls/Rate/Rate window 1", 28.0f, 0.0f, 42.0f, 28.0f, "1m", 14.0f, "#a7aa95", "left");
            Group("02 \u00b7 Header controls/Rate/Rate window 2", "02 \u00b7 Header controls/Rate", 88.0f, 24.0f, 70.0f, 28.0f);
            Sprite("02 \u00b7 Header controls/Rate/Rate window 2/Radio selected", "02 \u00b7 Header controls/Rate/Rate window 2", 0.0f, 4.0f, 20.0f, 20.0f, "ResizeHandles_0", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Rate/Rate window 2/Option label", "02 \u00b7 Header controls/Rate/Rate window 2", 28.0f, 0.0f, 42.0f, 28.0f, "5m", 14.0f, "#eaddbf", "left");
            Group("02 \u00b7 Header controls/Rate/Rate window 3", "02 \u00b7 Header controls/Rate", 160.0f, 24.0f, 70.0f, 28.0f);
            Sprite("02 \u00b7 Header controls/Rate/Rate window 3/Radio unselected", "02 \u00b7 Header controls/Rate/Rate window 3", 0.0f, 4.0f, 20.0f, 20.0f, "ResizeHandles_1", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Rate/Rate window 3/Option label", "02 \u00b7 Header controls/Rate/Rate window 3", 28.0f, 0.0f, 42.0f, 28.0f, "10m", 14.0f, "#a7aa95", "left");
            Group("02 \u00b7 Header controls/Rate/Rate units", "02 \u00b7 Header controls/Rate", 16.0f, 54.0f, 216.0f, 24.0f);
            Sprite("02 \u00b7 Header controls/Rate/Rate units/Stepper frame", "02 \u00b7 Header controls/Rate/Rate units", 0.0f, 0.0f, 216.0f, 24.0f, "DecoTitleDark", true, "", "", 1.0f);
            Sprite("02 \u00b7 Header controls/Rate/Rate units/Decrease", "02 \u00b7 Header controls/Rate/Rate units", 5.0f, 2.0f, 20.0f, 20.0f, "NavButtons_0", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Rate/Rate units/Value", "02 \u00b7 Header controls/Rate/Rate units", 28.0f, 0.0f, 160.0f, 24.0f, "Per minute", 14.0f, "#eaddbf", "center");
            Sprite("02 \u00b7 Header controls/Rate/Rate units/Increase", "02 \u00b7 Header controls/Rate/Rate units", 191.0f, 2.0f, 20.0f, 20.0f, "NavButtons_1", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Rate/Rolling calculation label", "02 \u00b7 Header controls/Rate", 16.0f, 80.0f, 184.0f, 24.0f, "Rolling calculation", 14.0f, "#eaddbf", "left");
            Sprite("02 \u00b7 Header controls/Rate/Rolling calculation toggle", "02 \u00b7 Header controls/Rate", 212.0f, 82.0f, 20.0f, 20.0f, "ResizeHandles_0", false, "", "", 1.0f);
            Group("02 \u00b7 Header controls/Mode", "02 \u00b7 Header controls", 276.0f, 82.0f, 136.0f, 110.0f);
            Sprite("02 \u00b7 Header controls/Mode/Control background", "02 \u00b7 Header controls/Mode", 0.0f, 0.0f, 136.0f, 110.0f, "DecoRectDark", true, "#2d382f", "#9e875b", 1.0f);
            Group("02 \u00b7 Header controls/Mode/Control heading", "02 \u00b7 Header controls/Mode", 16.0f, -10.0f, 100.0f, 30.0f);
            Sprite("02 \u00b7 Header controls/Mode/Control heading/Gold title plaque", "02 \u00b7 Header controls/Mode/Control heading", 0.0f, 0.0f, 100.0f, 30.0f, "DecoTitleYellow", true, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Mode/Control heading/Section label", "02 \u00b7 Header controls/Mode/Control heading", 10.0f, 0.0f, 80.0f, 30.0f, "Mode", 14.0f, "#302718", "center");
            Group("02 \u00b7 Header controls/Mode/Basic mode", "02 \u00b7 Header controls/Mode", 18.0f, 24.0f, 100.0f, 28.0f);
            Sprite("02 \u00b7 Header controls/Mode/Basic mode/Radio selected", "02 \u00b7 Header controls/Mode/Basic mode", 0.0f, 4.0f, 20.0f, 20.0f, "ResizeHandles_0", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Mode/Basic mode/Option label", "02 \u00b7 Header controls/Mode/Basic mode", 28.0f, 0.0f, 72.0f, 28.0f, "Basic", 14.0f, "#eaddbf", "left");
            Group("02 \u00b7 Header controls/Mode/Advanced mode", "02 \u00b7 Header controls/Mode", 18.0f, 70.0f, 100.0f, 28.0f);
            Sprite("02 \u00b7 Header controls/Mode/Advanced mode/Radio unselected", "02 \u00b7 Header controls/Mode/Advanced mode", 0.0f, 4.0f, 20.0f, 20.0f, "ResizeHandles_1", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Mode/Advanced mode/Option label", "02 \u00b7 Header controls/Mode/Advanced mode", 28.0f, 0.0f, 72.0f, 28.0f, "Advanced", 14.0f, "#a7aa95", "left");
            Group("02 \u00b7 Header controls/Sort", "02 \u00b7 Header controls", 424.0f, 82.0f, 124.0f, 110.0f);
            Sprite("02 \u00b7 Header controls/Sort/Control background", "02 \u00b7 Header controls/Sort", 0.0f, 0.0f, 124.0f, 110.0f, "DecoRectDark", true, "#2d382f", "#9e875b", 1.0f);
            Group("02 \u00b7 Header controls/Sort/Control heading", "02 \u00b7 Header controls/Sort", 16.0f, -10.0f, 100.0f, 30.0f);
            Sprite("02 \u00b7 Header controls/Sort/Control heading/Gold title plaque", "02 \u00b7 Header controls/Sort/Control heading", 0.0f, 0.0f, 100.0f, 30.0f, "DecoTitleYellow", true, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Sort/Control heading/Section label", "02 \u00b7 Header controls/Sort/Control heading", 10.0f, 0.0f, 80.0f, 30.0f, "Sort", 14.0f, "#302718", "center");
            Group("02 \u00b7 Header controls/Sort/Sort field", "02 \u00b7 Header controls/Sort", 16.0f, 24.0f, 92.0f, 30.0f);
            Sprite("02 \u00b7 Header controls/Sort/Sort field/Selector frame", "02 \u00b7 Header controls/Sort/Sort field", 0.0f, 0.0f, 92.0f, 30.0f, "DecoTitleDark", true, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Sort/Sort field/Selected value", "02 \u00b7 Header controls/Sort/Sort field", 8.0f, 0.0f, 64.0f, 30.0f, "Tier / Name", 12.0f, "#eaddbf", "left");
            Sprite("02 \u00b7 Header controls/Sort/Sort field/Dropdown caret", "02 \u00b7 Header controls/Sort/Sort field", 72.0f, 9.0f, 12.0f, 12.0f, "DropdownArrow", false, "", "", 1.0f);
            Group("02 \u00b7 Header controls/Sort/Sort direction", "02 \u00b7 Header controls/Sort", 16.0f, 70.0f, 92.0f, 30.0f);
            Sprite("02 \u00b7 Header controls/Sort/Sort direction/Selector frame", "02 \u00b7 Header controls/Sort/Sort direction", 0.0f, 0.0f, 92.0f, 30.0f, "DecoTitleDark", true, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Sort/Sort direction/Selected value", "02 \u00b7 Header controls/Sort/Sort direction", 8.0f, 0.0f, 64.0f, 30.0f, "Ascending", 12.0f, "#eaddbf", "left");
            Sprite("02 \u00b7 Header controls/Sort/Sort direction/Dropdown caret", "02 \u00b7 Header controls/Sort/Sort direction", 72.0f, 9.0f, 12.0f, 12.0f, "DropdownArrow", false, "", "", 1.0f);
            Group("02 \u00b7 Header controls/Display", "02 \u00b7 Header controls", 560.0f, 82.0f, 192.0f, 110.0f);
            Sprite("02 \u00b7 Header controls/Display/Control background", "02 \u00b7 Header controls/Display", 0.0f, 0.0f, 192.0f, 110.0f, "DecoRectDark", true, "#2d382f", "#9e875b", 1.0f);
            Group("02 \u00b7 Header controls/Display/Control heading", "02 \u00b7 Header controls/Display", 16.0f, -10.0f, 107.0f, 30.0f);
            Sprite("02 \u00b7 Header controls/Display/Control heading/Gold title plaque", "02 \u00b7 Header controls/Display/Control heading", 0.0f, 0.0f, 107.0f, 30.0f, "DecoTitleYellow", true, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Display/Control heading/Section label", "02 \u00b7 Header controls/Display/Control heading", 10.0f, 0.0f, 87.0f, 30.0f, "Display", 14.0f, "#302718", "center");
            Text("02 \u00b7 Header controls/Display/Refresh label", "02 \u00b7 Header controls/Display", 14.0f, 24.0f, 104.0f, 26.0f, "Refresh (sec)", 14.0f, "#eaddbf", "left");
            Group("02 \u00b7 Header controls/Display/Refresh interval", "02 \u00b7 Header controls/Display", 126.0f, 24.0f, 52.0f, 26.0f);
            Sprite("02 \u00b7 Header controls/Display/Refresh interval/Stepper frame", "02 \u00b7 Header controls/Display/Refresh interval", 0.0f, 0.0f, 52.0f, 26.0f, "DecoTitleDark", true, "", "", 1.0f);
            Sprite("02 \u00b7 Header controls/Display/Refresh interval/Decrease", "02 \u00b7 Header controls/Display/Refresh interval", 3.0f, 6.0f, 14.0f, 14.0f, "NavButtons_0", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Display/Refresh interval/Value", "02 \u00b7 Header controls/Display/Refresh interval", 18.0f, 0.0f, 16.0f, 26.0f, "1", 14.0f, "#eaddbf", "center");
            Sprite("02 \u00b7 Header controls/Display/Refresh interval/Increase", "02 \u00b7 Header controls/Display/Refresh interval", 35.0f, 6.0f, 14.0f, 14.0f, "NavButtons_1", false, "", "", 1.0f);
            Sprite("02 \u00b7 Header controls/Display/Freeze toggle", "02 \u00b7 Header controls/Display", 158.0f, 54.0f, 20.0f, 20.0f, "ResizeHandles_1", false, "", "", 1.0f);
            Text("02 \u00b7 Header controls/Display/Freeze label", "02 \u00b7 Header controls/Display", 14.0f, 52.0f, 132.0f, 24.0f, "Freeze display", 14.0f, "#eaddbf", "left");
            Text("02 \u00b7 Header controls/Display/Rolling average label", "02 \u00b7 Header controls/Display", 14.0f, 80.0f, 164.0f, 20.0f, "Avg: 3 samples", 12.0f, "#a7aa95", "left");
            Block("Header divider", "", 24.0f, 222.0f, 768.0f, 2.0f, "#d0b177", 1.0f);
            Group("03 \u00b7 Table toolbar", "", 40.0f, 232.0f, 736.0f, 34.0f);
            Text("03 \u00b7 Table toolbar/Table title", "03 \u00b7 Table toolbar", 0.0f, 0.0f, 350.0f, 34.0f, "ECONOMY & PRODUCTION", 16.0f, "#e2bd77", "left");
            Group("03 \u00b7 Table toolbar/Search", "03 \u00b7 Table toolbar", 366.0f, 2.0f, 220.0f, 30.0f);
            Sprite("03 \u00b7 Table toolbar/Search/Selector frame", "03 \u00b7 Table toolbar/Search", 0.0f, 0.0f, 220.0f, 30.0f, "DecoTitleDark", true, "", "", 1.0f);
            Text("03 \u00b7 Table toolbar/Search/Selected value", "03 \u00b7 Table toolbar/Search", 12.0f, 0.0f, 182.0f, 30.0f, "Search assets...", 13.0f, "#eaddbf", "left");
            Sprite("03 \u00b7 Table toolbar/Search/Dropdown caret", "03 \u00b7 Table toolbar/Search", 194.0f, 9.0f, 12.0f, 12.0f, "DropdownArrow", false, "", "", 1.0f);
            Sprite("03 \u00b7 Table toolbar/Show all checkbox", "03 \u00b7 Table toolbar", 600.0f, 8.0f, 20.0f, 20.0f, "Checkbox_0", false, "", "", 1.0f);
            Text("03 \u00b7 Table toolbar/Show all label", "03 \u00b7 Table toolbar", 630.0f, 0.0f, 106.0f, 34.0f, "Show all", 13.0f, "#eaddbf", "left");
            Group("04 \u00b7 Ledger table", "", 24.0f, 274.0f, 768.0f, 448.0f);
            Sprite("04 \u00b7 Ledger table/Ledger frame", "04 \u00b7 Ledger table", 0.0f, 0.0f, 768.0f, 448.0f, "DecoRectDark", true, "#28242a", "#81755f", 1.0f);
            Group("04 \u00b7 Ledger table/Column headings", "04 \u00b7 Ledger table", 16.0f, 8.0f, 736.0f, 32.0f);
            Block("04 \u00b7 Ledger table/Column headings/Header background", "04 \u00b7 Ledger table/Column headings", 0.0f, 0.0f, 736.0f, 32.0f, "#3b4337", 1.0f);
            Text("04 \u00b7 Ledger table/Column headings/Asset heading", "04 \u00b7 Ledger table/Column headings", 16.0f, 0.0f, 232.0f, 32.0f, "ASSET / SOURCE", 12.0f, "#e2bd77", "left");
            Text("04 \u00b7 Ledger table/Column headings/Income heading", "04 \u00b7 Ledger table/Column headings", 254.0f, 0.0f, 112.0f, 32.0f, "IN / MIN", 12.0f, "#e2bd77", "right");
            Text("04 \u00b7 Ledger table/Column headings/Outflow heading", "04 \u00b7 Ledger table/Column headings", 378.0f, 0.0f, 112.0f, 32.0f, "OUT / MIN", 12.0f, "#e2bd77", "right");
            Text("04 \u00b7 Ledger table/Column headings/Net heading", "04 \u00b7 Ledger table/Column headings", 502.0f, 0.0f, 128.0f, 32.0f, "NET / MIN", 12.0f, "#e2bd77", "right");
            Group("05 \u00b7 Status and footer", "", 24.0f, 738.0f, 768.0f, 100.0f);
            Text("05 \u00b7 Status and footer/Observation window", "05 \u00b7 Status and footer", 16.0f, 0.0f, 456.0f, 28.0f, "Last 30 seconds  \u00b7  Per-minute rates", 13.0f, "#a7aa95", "left");
            Text("05 \u00b7 Status and footer/Row count", "05 \u00b7 Status and footer", 510.0f, 0.0f, 242.0f, 28.0f, "7 active assets", 13.0f, "#a7aa95", "right");
            Block("05 \u00b7 Status and footer/Footer divider", "05 \u00b7 Status and footer", 0.0f, 40.0f, 768.0f, 1.0f, "#615844", 1.0f);
            Group("05 \u00b7 Status and footer/Close ledger", "05 \u00b7 Status and footer", 280.0f, 50.0f, 208.0f, 48.0f);
            Sprite("05 \u00b7 Status and footer/Close ledger/Button frame", "05 \u00b7 Status and footer/Close ledger", 0.0f, 0.0f, 208.0f, 48.0f, "Button03", true, "", "", 1.0f);
            Text("05 \u00b7 Status and footer/Close ledger/Button label", "05 \u00b7 Status and footer/Close ledger", 12.0f, 0.0f, 184.0f, 48.0f, "Close", 14.0f, "#eaddbf", "center");
            Group("Advanced toolbar", "", 40.0f, 232.0f, 736.0f, 34.0f);
            Group("Advanced toolbar/Currencies tab", "Advanced toolbar", 0.0f, 0.0f, 120.0f, 34.0f);
            Sprite("Advanced toolbar/Currencies tab/Button frame", "Advanced toolbar/Currencies tab", 0.0f, 0.0f, 120.0f, 34.0f, "Button02", true, "", "", 1.0f);
            Text("Advanced toolbar/Currencies tab/Button label", "Advanced toolbar/Currencies tab", 12.0f, 0.0f, 96.0f, 34.0f, "Currencies", 14.0f, "#e2bd77", "center");
            Group("Advanced toolbar/Production tab", "Advanced toolbar", 128.0f, 0.0f, 120.0f, 34.0f);
            Sprite("Advanced toolbar/Production tab/Button frame", "Advanced toolbar/Production tab", 0.0f, 0.0f, 120.0f, 34.0f, "Button03", true, "", "", 1.0f);
            Text("Advanced toolbar/Production tab/Button label", "Advanced toolbar/Production tab", 12.0f, 0.0f, 96.0f, 34.0f, "Production", 14.0f, "#eaddbf", "center");
            Group("Advanced toolbar/Collapse rows", "Advanced toolbar", 256.0f, 0.0f, 104.0f, 34.0f);
            Sprite("Advanced toolbar/Collapse rows/Button frame", "Advanced toolbar/Collapse rows", 0.0f, 0.0f, 104.0f, 34.0f, "Button03", true, "", "", 1.0f);
            Text("Advanced toolbar/Collapse rows/Button label", "Advanced toolbar/Collapse rows", 12.0f, 0.0f, 80.0f, 34.0f, "Collapse all", 13.0f, "#eaddbf", "center");
        }

        internal RectTransform Node(string suffix) => nodes.Single(pair => pair.Key == suffix || pair.Key.EndsWith("/" + suffix, StringComparison.Ordinal)).Value;
        internal TMP_Text Label(string suffix) => Node(suffix).GetComponent<TMP_Text>();
        internal void StyleOverviewButton(Button button)
        {
            var rect = (RectTransform)button.transform;
            GlobalLedgerUi.Place(rect, 12, 8, 344, 36);
            var image = button.GetComponent<Image>();
            image.sprite = assets.Get("Button03");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1;
            image.color = UnityEngine.Color.white;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = UnityEngine.Color.white;
            colors.highlightedColor = new Color(1, .94f, .78f);
            colors.pressedColor = new Color(.8f, .75f, .65f);
            button.colors = colors;
            var label = Label("Overview heading");
            GlobalLedgerUi.Fill(label.rectTransform, 24, 0, 44, 0);
            label.fontSize = label.fontSizeMax = 20;
            label.fontSizeMin = 18;
            label.enableAutoSizing = true;
            overviewArrow = GlobalLedgerUi.Rect(rect, "CollapseIndicator");
            GlobalLedgerUi.Place(overviewArrow, 0, 0, 12, 12);
            overviewArrow.pivot = new Vector2(.5f, .5f);
            overviewArrow.anchoredPosition = new Vector2(316, -18);
            CreateIndicatorStroke(-3, -45);
            CreateIndicatorStroke(3, 45);
        }
        void CreateIndicatorStroke(float x, float angle)
        {
            var stroke = GlobalLedgerUi.Rect(overviewArrow, "Stroke");
            stroke.anchorMin = stroke.anchorMax = stroke.pivot = new Vector2(.5f, .5f);
            stroke.anchoredPosition = new Vector2(x, 1);
            stroke.sizeDelta = new Vector2(8, 2);
            stroke.localEulerAngles = new Vector3(0, 0, angle);
            var image = stroke.gameObject.AddComponent<Image>();
            image.color = GlobalLedgerUi.Gold;
            image.raycastTarget = false;
        }
        internal void SetControlsCollapsed(bool collapsed)
        {
            overviewArrow.localEulerAngles = new Vector3(0, 0, collapsed ? 90 : 0);
            foreach (string section in new[] { "Rate", "Mode", "Sort", "Display" })
                Node(section).gameObject.SetActive(!collapsed);
            float shift = collapsed ? ControlsHeight : 0;
            SetHeight("01 \u00b7 Window frame", Height - shift);
            SetHeight("Main panel", Height - shift);
            SetHeight("02 \u00b7 Header controls", 196 - shift);
            SetHeight("02 \u00b7 Header controls/Header background", 196 - shift);
            SetTop("Header divider", 222 - shift);
            SetTop("03 \u00b7 Table toolbar", 232 - shift);
            SetTop("Advanced toolbar", 232 - shift);
            SetTop("04 \u00b7 Ledger table", 274 - shift);
            SetTop("05 \u00b7 Status and footer", 738 - shift);
        }
        void SetHeight(string name, float height)
        {
            var node = Node(name);
            node.sizeDelta = new Vector2(node.sizeDelta.x, height);
        }
        void SetTop(string name, float top)
        {
            var node = Node(name);
            node.anchoredPosition = new Vector2(node.anchoredPosition.x, -top);
        }
        internal static Color Color(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out var value))
                throw new ArgumentException("Invalid ledger color: " + hex, nameof(hex));
            return value;
        }
        internal Sprite Asset(string name) => assets.Get(name);
        RectTransform Create(string key, string parent, float x, float y, float w, float h)
        {
            var rect = GlobalLedgerUi.Rect(parent.Length == 0 ? root : nodes[parent], key.Split('/').Last());
            GlobalLedgerUi.Place(rect, x, y, w, h); nodes.Add(key, rect); return rect;
        }
        void Group(string key, string parent, float x, float y, float w, float h) => Create(key, parent, x, y, w, h);
        void Sprite(string key, string parent, float x, float y, float w, float h, string asset, bool sliced, string fill, string border, float opacity)
        {
            var image = Create(key, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = assets.Get(asset, fill, border);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = .5f;
            image.color = new Color(1, 1, 1, opacity); image.raycastTarget = false;
        }
        void Block(string key, string parent, float x, float y, float w, float h, string color, float opacity)
        {
            var image = Create(key, parent, x, y, w, h).gameObject.AddComponent<Image>();
            var ink = Color(color); ink.a = opacity; image.color = ink; image.raycastTarget = false;
        }
        void Text(string key, string parent, float x, float y, float w, float h, string value, float size, string color, string alignment)
        {
            var rect = Create(key, parent, x, y, w, h);
            var label = ui.Text(rect, "Text", value, size); GlobalLedgerUi.Fill(label.rectTransform);
            label.color = Color(color);
            label.alignment = alignment == "right" ? TextAlignmentOptions.MidlineRight : alignment == "center" ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            // Store the text's rect so text bindings address the component directly.
            nodes[key] = label.rectTransform;
        }
        internal Button Click(string suffix, Action action)
        {
            var node = Node(suffix);
            if (node.GetComponent<TMP_Text>()) node = (RectTransform)node.parent;
            var image = node.GetComponent<Image>() ?? node.gameObject.AddComponent<Image>();
            if (!image.sprite) image.color = UnityEngine.Color.clear;
            image.raycastTarget = true;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); FoundationApi.UiAccess.PlaySound(GameUiSound.Button); action(); });
            return button;
        }
        internal void Radio(string suffix, bool selected)
        {
            var icon = Node(suffix).GetComponentsInChildren<Image>().FirstOrDefault(image => image.sprite && image.sprite.name.StartsWith("ResizeHandles", StringComparison.Ordinal));
            if (!icon) icon = Node(suffix).GetComponent<Image>();
            icon.sprite = assets.Get(selected ? "ResizeHandles_0" : "ResizeHandles_1");
        }
        internal void Choices(string suffix, string[] values, Action<int> selected)
        {
            CloseChoices();
            menu = GlobalLedgerUi.Rect(root, "LedgerChoices");
            Vector3 point = root.InverseTransformPoint(Node(suffix).TransformPoint(new Vector3(0, -Node(suffix).rect.height, 0)));
            GlobalLedgerUi.Place(menu, point.x, -point.y, Node(suffix).rect.width, values.Length * 30 + 8);
            var background = ui.Background(menu, Color("#30282f"));
            for (int i = 0; i < values.Length; i++)
            {
                int choice = i;
                var button = ui.Button(menu, "Choice" + i, values[i], () => { CloseChoices(); selected(choice); }, 13);
                GlobalLedgerUi.Place((RectTransform)button.transform, 4, 4 + i * 30, menu.sizeDelta.x - 8, 30);
            }
        }
        internal void CloseChoices() { if (menu) UnityEngine.Object.Destroy(menu.gameObject); menu = null; }
        internal TMP_InputField Search(Action<string> changed)
        {
            var group = Node("Search");
            Node("Search/Dropdown caret").gameObject.SetActive(false);
            var value = Label("Search/Selected value");
            value.text = "";
            var viewport = GlobalLedgerUi.Rect(group, "InputViewport"); GlobalLedgerUi.Place(viewport, 12, 0, 196, 30);
            viewport.gameObject.AddComponent<RectMask2D>();
            value.transform.SetParent(viewport, false); GlobalLedgerUi.Fill(value.rectTransform);
            var placeholder = ui.Text(viewport, "Placeholder", "Search assets...", 13); placeholder.color = GlobalLedgerUi.Paper;
            var field = group.gameObject.AddComponent<TMP_InputField>();
            var hit = group.gameObject.AddComponent<Image>(); hit.color = UnityEngine.Color.clear;
            field.targetGraphic = hit; field.textViewport = viewport; field.textComponent = value; field.placeholder = placeholder;
            field.characterLimit = 100; field.onValueChanged.AddListener(text => changed(text));
            return field;
        }
        public void Dispose() => assets.Dispose();
    }
}
