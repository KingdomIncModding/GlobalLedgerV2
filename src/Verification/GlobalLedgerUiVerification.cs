using System;
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2.Verification
{
    internal sealed class GlobalLedgerUiVerification
    {
        GlobalLedgerPanel panel;
        GameObject canvasRoot;
        GamePlayer original;
        JsonArray checks;
        string output;
        internal bool Passed = true;
        internal string Failure;
        internal IEnumerator Run(string output, JsonArray checks)
        {
            this.output = output; this.checks = checks;
            if (!Try(Setup)) { Cleanup(); yield break; }
            yield return new WaitForSecondsRealtime(1.5f);
            Try(() => Capture("basic-1920.png"));
            yield return new WaitForSecondsRealtime(1);
            Try(VerifyBasic);
            Try(VerifyCollapsedControls);
            yield return new WaitForSecondsRealtime(.3f);
            Try(() => Capture("basic-collapsed-1920.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Try(VerifyRestoredControls);
            Try(() => panel.SetTimeUnit(GlobalLedgerPanel.TimeUnit.Sec));
            yield return new WaitForSecondsRealtime(.3f);
            Try(() => Capture("rates-seconds.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Try(() => panel.SetTimeUnit(GlobalLedgerPanel.TimeUnit.Hour));
            yield return new WaitForSecondsRealtime(.3f);
            Try(() => Capture("rates-hours.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Try(() => panel.SetTimeUnit(GlobalLedgerPanel.TimeUnit.Min));
            Try(() => { panel.SetViewMode(GlobalLedgerPanel.ViewMode.Adv); panel.ToggleRow("currency/0"); foreach (var row in panel.VisibleRows.Where(row => row.Depth == 1 && row.Expandable).ToArray()) panel.ToggleRow(row.Key); });
            yield return new WaitForSecondsRealtime(.5f);
            Try(() => Capture("advanced-1920.png"));
            yield return new WaitForSecondsRealtime(1);
            Try(VerifyNameScrolling);
            Try(() =>
            {
                var name = ScrollingFixture();
                name.Bind("scroll-preview", "01 FOREST DEFENSE - Eastern Frontier War Effort and Production");
                name.OnPointerEnter(null);
            });
            yield return new WaitForSecondsRealtime(3);
            Try(() => Capture("hover-name-scroll.png"));
            yield return new WaitForSecondsRealtime(.3f);
            Try(() => { ScrollingFixture().OnPointerExit(null); panel.RefreshNow(); });
            Try(VerifyCollapsedControls);
            yield return new WaitForSecondsRealtime(.3f);
            Try(() => Capture("advanced-collapsed-1920.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Try(VerifyRestoredControls);
            Try(VerifyControls);
            Screen.SetResolution(1280, 720, false);
            yield return new WaitForSecondsRealtime(1);
            Try(() => { panel.SetViewMode(GlobalLedgerPanel.ViewMode.Basic); Capture("basic-1280.png"); VerifyFrameFits(); VerifyCoinColumns(); });
            yield return new WaitForSecondsRealtime(1);
            Try(VerifyCollapsedControls);
            yield return new WaitForSecondsRealtime(.3f);
            Try(() => { Capture("basic-collapsed-1280.png"); VerifyFrameFits(); });
            yield return new WaitForSecondsRealtime(.5f);
            Try(VerifyRestoredControls);
            Try(() =>
            {
                foreach (string name in new[] { "basic-1920.png", "advanced-1920.png", "basic-1280.png", "rates-seconds.png", "rates-hours.png", "basic-collapsed-1920.png", "advanced-collapsed-1920.png", "basic-collapsed-1280.png" }) Check(File.Exists(Path.Combine(output, name)), "Rendered " + name);
            });
            Cleanup();
        }
        bool Try(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Passed = false; Failure = (Failure ?? "") + error + "\n"; return false; }
        }
        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            checks.Add(name);
        }
        void Setup()
        {
            Screen.SetResolution(1920, 1080, false);
            original = GamePlayer.Current;
            var player = new GamePlayer();
            var unlocked = (bool[])AccessTools.Field(typeof(GamePlayer), "_techUnlocked").GetValue(player);
            for (int i = 0; i < unlocked.Length; i++) unlocked[i] = true;
            var flow = new FlowDiagram { Label = "Ironworks" }; player.Flows.Add(flow);
            GamePlayer.Current = player; var plugin = GlobalLedgerV2Plugin.Instance; plugin.EnsurePlayer();
            plugin.EnableRollingAverage.Value = false;
            var node = new GlobalLedgerFixtureNode { Position = new Vector2Int(128, -128) }; flow.Elements.Add(node);
            flow.Decorations.Add(new FlowDeco { Position = Vector2Int.zero, Size = new Vector2Int(512, 512) });
            flow.Decorations.Add(new FlowDeco { Label = "Ironworks", Position = new Vector2Int(10, -10), Size = new Vector2Int(64, 32) });
            var source = new GlobalLedgerBookSource(node.Guid, "shop", "Item Shop", "Item Shop");
            plugin.Book.Record(source, GameCurrency.Gold, 710, 90);
            plugin.Book.Record(source, GameCurrency.Mana, 42, 8);
            plugin.Book.Record(source, GameCurrency.Requisition, 6, 10);
            string[] items = { "iron_ore", "iron_ingot", "iron_sword", "linen_cloth" };
            long[] earned = { 24, 16, 6, 12 }, spent = { 16, 14, 4, 9 };
            for (int i = 0; i < items.Length; i++) plugin.Book.RecordItem(source, items[i], earned[i], spent[i]);
            canvasRoot = new GameObject("LedgerVisualVerification", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
            var background = GlobalLedgerUi.Rect(canvasRoot.transform, "Backdrop"); GlobalLedgerUi.Fill(background);
            background.gameObject.AddComponent<Image>().color = GlobalLedgerDesign.Color("#11110f");
            var anchor = GlobalLedgerUi.Rect(canvasRoot.transform, "Anchor"); GlobalLedgerUi.Place(anchor, 1100, 0, 312, 40);
            anchor.anchorMin = anchor.anchorMax = anchor.pivot = new Vector2(1, 1); anchor.anchoredPosition = new Vector2(-508, 0);
            var tabImage = anchor.gameObject.AddComponent<Image>(); tabImage.sprite = new GlobalLedgerDesignAssets().Get("DecoTitleRed"); tabImage.type = Image.Type.Sliced; tabImage.pixelsPerUnitMultiplier = .5f;
            var button = anchor.gameObject.AddComponent<Button>();
            var text = GlobalLedgerUi.Rect(anchor, "Label").gameObject.AddComponent<TextMeshProUGUI>();
            GlobalLedgerUi.Fill(text.rectTransform); text.font = TMP_Settings.defaultFontAsset; text.text = "Global Ledger"; text.fontSize = 24; text.color = GlobalLedgerUi.Paper; text.alignment = TextAlignmentOptions.Center;
            Canvas.ForceUpdateCanvases();
            panel = GlobalLedgerPanel.Create(plugin.Book, button); panel.OpenPopup();
            // Disable only the anchor visibility watchdog in this synthetic scene.
            panel.enabled = false;
        }
        GlobalLedgerNameScroll ScrollingFixture() => panel.Scroller.content.GetComponentsInChildren<GlobalLedgerNameScroll>()
            .First(value => ((RectTransform)value.transform).rect.width < 300);

        void VerifyNameScrolling()
        {
            var name = ScrollingFixture();
            var label = name.GetComponentInChildren<TMP_Text>();
            var rect = (RectTransform)name.transform;
            var position = rect.anchoredPosition;
            var size = rect.sizeDelta;
            name.enabled = false;
            const string full = "01 FOREST DEFENSE - Eastern Frontier War Effort and Production";
            try
            {
                Check(name.GetComponent<RectMask2D>() && name.GetComponent<Image>().raycastTarget, "Names have a clipped, pointer-sensitive viewport");
                name.Bind("scroll-fixture", full);
                name.OnPointerEnter(null);
                name.Advance(1.9f);
                label.ForceMeshUpdate();
                Check(label.overflowMode == TextOverflowModes.Ellipsis && label.rectTransform.anchoredPosition == Vector2.zero && label.isTextTruncated,
                    "Long names remain ellipsized during the two-second hover delay");
                name.Advance(1.1f);
                Check(label.text == full && label.overflowMode == TextOverflowModes.Overflow && Mathf.Abs(label.rectTransform.anchoredPosition.x + 32) < .1f,
                    "Hovered names scroll horizontally at a readable speed after two seconds");
                float offset = label.rectTransform.anchoredPosition.x;
                name.Bind("scroll-fixture", full); name.Layout();
                Check(label.rectTransform.anchoredPosition.x == offset, "Unchanged refreshes preserve the current name-scroll position");
                Check(rect.anchoredPosition == position && rect.sizeDelta == size && label.rectTransform.rect.width > rect.rect.width,
                    "Scrolling leaves the name column and amount columns fixed");
                float distance = label.GetPreferredValues(full, Mathf.Infinity, Mathf.Infinity).x - rect.rect.width;
                float travel = distance / GlobalLedgerNameScroll.Speed;
                name.OnPointerExit(null); name.OnPointerEnter(null);
                name.Advance(GlobalLedgerNameScroll.HoverDelay + travel + .5f);
                Check(Mathf.Abs(label.rectTransform.anchoredPosition.x + distance) < .1f, "Name scrolling pauses with the final characters visible");
                name.Advance(GlobalLedgerNameScroll.EndPause);
                Check(label.rectTransform.anchoredPosition.x > -distance, "Names return smoothly after the end pause");
                name.OnPointerExit(null);
                Check(label.overflowMode == TextOverflowModes.Ellipsis && label.rectTransform.anchoredPosition == Vector2.zero && label.rectTransform.rect.width == rect.rect.width,
                    "Pointer exit immediately restores the ellipsis and original position");
                name.Advance(10);
                Check(label.rectTransform.anchoredPosition == Vector2.zero, "Names remain still when not hovered");
                name.OnPointerEnter(null); name.Advance(3);
                name.Bind("recycled-row", full);
                Check(label.overflowMode == TextOverflowModes.Ellipsis && label.rectTransform.anchoredPosition == Vector2.zero,
                    "Recycled rows restart the hover delay for their new identity");
                name.OnPointerExit(null); name.Bind("short-row", "Ore"); name.OnPointerEnter(null); name.Advance(10);
                Check(label.overflowMode == TextOverflowModes.Ellipsis && label.rectTransform.anchoredPosition == Vector2.zero,
                    "Names that fit do not scroll");
                name.Bind("scroll-fixture", full); name.Advance(3);
                name.SendMessage("OnDisable");
                Check(label.overflowMode == TextOverflowModes.Ellipsis && label.rectTransform.anchoredPosition == Vector2.zero,
                    "Hidden rows reset their scrolling state");
            }
            finally { name.OnPointerExit(null); name.enabled = true; panel.RefreshNow(); }
        }

        void VerifyBasic()
        {
            Check(panel.Frame.sizeDelta == new Vector2(816, 856), "Slim window is 816 by 856");
            Check(panel.Frame.anchoredPosition == new Vector2(624, -48), "Ledger is anchored below the ModBar button with designer horizontal alignment");
            Check(!panel.Frame.GetComponentsInChildren<RectTransform>(true).Any(rect => rect.name == "Red title tab"), "The popup has no duplicate decorative ModBar tab");
            Check(panel.Scroller.viewport.anchoredPosition == new Vector2(40, -322), "Table viewport matches designer insets");
            var gold = panel.VisibleRows.Single(row => row.GameCurrency == GameCurrency.Gold);
            Check(gold.Income == 710 && gold.Outflow == 90 && gold.Amount == 620 && panel.Rate(gold.Amount) == 620, "In, Out and Net use actual accounting and normalize the selected window");
            Check(panel.VisibleRows.Count(row => row.IsHeader) == 2, "Basic mode retains two section headings");
            float top = 0;
            foreach (var row in panel.VisibleRows) { Check(row.Top == top, "Aligned row " + row.Key); top += row.IsHeader ? 28 : 48; }
            VerifyFrameFits();
            VerifySlimControls();
        }
        void ClickOverview() => panel.Design.Label("Overview heading").GetComponentInParent<Button>().onClick.Invoke();
        void VerifyCollapsedControls()
        {
            int window = panel.WindowSeconds;
            var unit = panel.Unit;
            string totals = string.Join(",", panel.VisibleRows.Select(row => row.Key + ":" + row.Amount));
            ClickOverview();
            Check(panel.ControlsCollapsed, "Clicking the overview heading collapses controls in " + panel.Mode);
            foreach (string section in new[] { "Rate", "Mode", "Sort", "Display" })
                Check(!panel.Design.Node(section).gameObject.activeInHierarchy, "Collapsed header hides " + section);
            Check(panel.Design.Label("Overview heading").gameObject.activeInHierarchy && panel.Design.Label("Live update status").gameObject.activeInHierarchy,
                "Heading and live status remain visible when controls are collapsed");
            Check(panel.Frame.sizeDelta == new Vector2(816, 736) && panel.Scroller.viewport.anchoredPosition == new Vector2(40, -202),
                "Collapsed window removes the control space and moves the table up");
            Check(panel.Design.Node("Header divider").anchoredPosition.y == -102 && panel.Design.Node("04 \u00b7 Ledger table").anchoredPosition.y == -154,
                "Collapsed table frame and header divider stay aligned");
            Check(panel.WindowSeconds == window && panel.Unit == unit && totals == string.Join(",", panel.VisibleRows.Select(row => row.Key + ":" + row.Amount)),
                "Collapsing controls preserves selected rates and ledger data");
            panel.RefreshNow();
            Check(panel.ControlsCollapsed && !panel.Design.Node("Rate").gameObject.activeSelf, "Ledger refresh keeps controls collapsed");
            var texture = panel.Design.Node("Main panel").GetComponent<Image>().sprite.texture;
            Check(((Color32)texture.GetPixel(texture.width / 2, texture.height / 2)).Equals((Color32)GlobalLedgerDesign.Color("#30282f")),
                "Collapsing preserves the original panel color");
        }
        void VerifyRestoredControls()
        {
            ClickOverview();
            Check(!panel.ControlsCollapsed && panel.Frame.sizeDelta == new Vector2(816, 856), "Clicking the heading again restores the full window");
            foreach (string section in new[] { "Rate", "Mode", "Sort", "Display" })
                Check(panel.Design.Node(section).gameObject.activeInHierarchy, "Expanded header restores " + section);
            Check(panel.Scroller.viewport.anchoredPosition == new Vector2(40, -322) && panel.Design.Node("Header divider").anchoredPosition.y == -222,
                "Expanding restores the original layout without accumulated offsets");
        }
        void VerifySlimControls()
        {
            var design = panel.Design;
            var heading = design.Label("Overview heading").GetComponentInParent<Button>();
            Check(heading.interactable && heading.GetComponent<Image>().sprite.name == "Button03" && heading.GetComponent<Image>().color.a == 1,
                "Overview is visibly styled as a native framed button");
            Check(-((RectTransform)heading.transform).anchoredPosition.y + ((RectTransform)heading.transform).rect.height <= -design.Label("Overview subtitle").transform.parent.GetComponent<RectTransform>().anchoredPosition.y,
                "Heading button and subtitle have separate vertical space");
            Check(design.Node("Refresh interval").rect.width == 52, "Refresh control is half its original 104-pixel width");
            Check(design.Node("Sort field").rect.width == 92 && design.Node("Sort direction").rect.width == 92, "Both Sort selectors are half their original 184-pixel width");
            Check(design.Node("Display").rect.width == 192 && design.Node("Sort").rect.width == 124, "Display and Sort sections are slimmer");
            foreach (string label in new[] { "Sort field/Selected value", "Sort direction/Selected value", "Refresh interval/Value" })
            {
                var text = design.Label(label); text.ForceMeshUpdate();
                Check(!text.isTextTruncated, "Compact control text fits: " + label);
            }
            var decrease = design.Node("Refresh interval/Decrease");
            var value = design.Node("Refresh interval/Value");
            var increase = design.Node("Refresh interval/Increase");
            Check(decrease.anchoredPosition.x + decrease.rect.width <= value.parent.GetComponent<RectTransform>().anchoredPosition.x &&
                value.parent.GetComponent<RectTransform>().anchoredPosition.x + value.rect.width <= increase.anchoredPosition.x,
                "Refresh arrows and value occupy separate space");
            Check(panel.Scroller.viewport.rect.width == 736, "Slim table viewport matches the row width");
        }
        void VerifyFrameFits()
        {
            var corners = new Vector3[4]; panel.Frame.GetWorldCorners(corners);
            Check(corners.All(point => point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height), "Entire frame fits " + Screen.width + "x" + Screen.height);
        }
        void VerifyCoinColumns()
        {
            var gold = panel.Scroller.content.GetComponentsInChildren<RectTransform>().First(rect => rect.name == "basic/currency/0");
            var incoming = (RectTransform)gold.Find("In/Coins");
            var outgoing = (RectTransform)gold.Find("Out/Coins");
            var net = (RectTransform)gold.Find("Net/Coins");
            var first = new Vector3[4]; var second = new Vector3[4]; var third = new Vector3[4];
            incoming.GetWorldCorners(first); outgoing.GetWorldCorners(second); net.GetWorldCorners(third);
            Check(first[3].x < second[0].x && second[3].x < third[0].x, "Large compact money values stay in separate columns at 1280x720");
            Check(incoming.Find("Amount0").gameObject.activeSelf && incoming.Find("Amount1").gameObject.activeSelf && !incoming.Find("Amount2").gameObject.activeSelf,
                "Gold amounts render gold and silver icons with copper hidden");
        }
        void VerifyControls()
        {
            Check(GlobalLedgerUi.SplitGoldCompact(10398) == ("+", 1L, 4L, 0L), "1 gold 3 silver 98 copper rounds to 1 gold 4 silver");
            Check(GlobalLedgerUi.SplitGoldCompact(10349) == ("+", 1L, 3L, 0L), "Copper below half a silver rounds down");
            Check(GlobalLedgerUi.SplitGoldCompact(10350) == ("+", 1L, 4L, 0L), "Half a silver rounds up");
            Check(GlobalLedgerUi.SplitGoldCompact(19998) == ("+", 2L, 0L, 0L), "Silver rounding carries into gold");
            Check(GlobalLedgerUi.SplitGoldCompact(-10398) == ("-", 1L, 4L, 0L), "Negative money rounds symmetrically and retains its sign");
            Check(GlobalLedgerUi.SplitGoldCompact(398) == ("+", 0L, 3L, 98L), "Amounts below gold retain silver and copper");
            Check(GlobalLedgerUi.SplitGoldCompact(98) == ("+", 0L, 0L, 98L) && GlobalLedgerUi.SplitGoldCompact(0) == ("", 0L, 0L, 0L), "Copper-only and zero amounts remain displayable");
            var design = panel.Design;
            design.Node("Search").GetComponent<TMP_InputField>().text = "Ironworks";
            Check(panel.VisibleRows.Any(row => row.Depth == 1 && row.Label.Contains("Ironworks")) && panel.VisibleRows.Where(row => row.Depth == 1).All(row => row.Label.Contains("Ironworks")), "Search finds sources through collapsed hierarchy");
            design.Node("Search").GetComponent<TMP_InputField>().text = "no-match-fixture";
            Check(panel.VisibleRows.Count == 0, "Search handles no matches");
            design.Node("Search").GetComponent<TMP_InputField>().text = "";
            design.Node("Freeze toggle").GetComponent<Button>().onClick.Invoke();
            string before = string.Join(",", panel.VisibleRows.Select(row => row.Amount));
            var source = new GlobalLedgerBookSource("freeze", "shop", "Shop", "Shop");
            GlobalLedgerV2Plugin.Instance.Book.Record(source, GameCurrency.Gold, 1000000, 0);
            panel.RefreshNow();
            Check(before == string.Join(",", panel.VisibleRows.Select(row => row.Amount)), "Freeze holds the displayed accounting snapshot");
            design.Node("Freeze toggle").GetComponent<Button>().onClick.Invoke();
            VerifyRateWindows();
            panel.CollapseAll();
            Check(panel.VisibleRows.All(row => row.Depth == 0), "Collapse all hides descendants");
            var ordering = new GlobalLedgerRowOrdering();
            var values = new[] { new GlobalLedgerRowData("a", "A", 0) { Amount = -2 }, new GlobalLedgerRowData("b", "B", 0) { Amount = 7 } };
            Check(ordering.Arrange(values, "", 4, true).First().Key == "b", "Net descending sort preserves numeric order");
            var smoothing = new GlobalLedgerSmoothing();
            var smooth = new GlobalLedgerRowData("smooth", "Smooth", 0) { GameCurrency = GameCurrency.Gold, Income = 10, Outflow = 2 };
            smoothing.Apply(new[] { smooth }, 3, true); smooth.Income = 20; smooth.Outflow = 4; smoothing.Apply(new[] { smooth }, 3, true);
            Check(smooth.Income == 15 && smooth.Outflow == 3 && smooth.Amount == 12, "Rolling average keeps Net equal to In minus Out");
        }
        void VerifyRateWindows()
        {
            int[][] expectedOptions = { new[] { 1, 10, 30 }, new[] { 1, 5, 10 }, new[] { 1, 12, 24 } };
            string[] suffixes = { "s", "m", "h" };
            int[] seconds = { 1, 60, 3600 };
            for (int unit = 0; unit < 3; unit++)
            {
                panel.SetTimeUnit((GlobalLedgerPanel.TimeUnit)unit);
                for (int index = 0; index < 3; index++)
                {
                    var name = "Rate window " + (index + 1);
                    var expected = expectedOptions[unit][index];
                    Check(panel.Design.Label(name + "/Option label").text == expected + suffixes[unit], "Rate label matches " + expected + suffixes[unit]);
                    panel.Design.Node(name).GetComponent<Button>().onClick.Invoke();
                    Check(panel.WindowSeconds == expected * seconds[unit] && panel.Rate(120) == 120d / expected,
                        "Window and normalized rate match " + expected + suffixes[unit]);
                }
            }
            panel.Design.Node("Rate units/Decrease").GetComponent<Button>().onClick.Invoke();
            Check(panel.Unit == GlobalLedgerPanel.TimeUnit.Min && panel.WindowSeconds == 600, "Unit stepper preserves the selected position and updates its duration");
            Check(panel.StatusText.Contains("Collecting "), "Partial window shows collecting progress (#4)");
            float scheduledRefresh = (float)AccessTools.Field(typeof(GlobalLedgerPanel), "nextRefresh").GetValue(panel);
            panel.Design.Node("Search").GetComponent<TMP_InputField>().text = "Ironworks";
            panel.Design.Node("Search").GetComponent<TMP_InputField>().text = "";
            float refreshAfterInteraction = (float)AccessTools.Field(typeof(GlobalLedgerPanel), "nextRefresh").GetValue(panel);
            Check(Mathf.Approximately(scheduledRefresh, refreshAfterInteraction), "Search refreshes do not postpone the scheduled accounting sample timer (#3)");
            panel.Design.Node("Rate window 1").GetComponent<Button>().onClick.Invoke();
        }
        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases(); panel.SendMessage("Fit"); Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
        }
        void Cleanup()
        {
            if (panel) UnityEngine.Object.Destroy(panel.gameObject);
            if (canvasRoot) UnityEngine.Object.Destroy(canvasRoot);
            GamePlayer.Current = original; GlobalLedgerV2Plugin.Instance.EnsurePlayer();
        }
    }
}
