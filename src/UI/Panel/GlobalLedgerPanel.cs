using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    public sealed class GlobalLedgerPanel : MonoBehaviour
    {
        internal void Activate(GlobalLedgerRowData row)
        {
            if (row.IsHeader) return;
            selectedKey = row.Key;
            if (row.Expandable) ToggleRow(row.Key);
            else RefreshNow();
        }

        GlobalLedgerRowData Add(string key, string label, int depth, GameCurrency currency, decimal amount, bool expandable, decimal income = 0, decimal outflow = 0)
        {
            var row = new GlobalLedgerRowData(key, label, depth)
            {
                GameCurrency = currency,
                Amount = amount,
                Income = income,
                Outflow = outflow,
                Expandable = expandable,
                Open = expanded.Contains(key),
                Icon = depth == 0 ? currency.GetIcon() : null
            };
            VisibleRows.Add(row);
            return row;
        }

        GlobalLedgerRowData AddItem(string key, string label, int depth, GameItemType item, decimal amount, bool expandable, decimal income = 0, decimal outflow = 0)
        {
            var row = new GlobalLedgerRowData(key, label, depth)
            {
                Item = item,
                Amount = amount,
                Income = income,
                Outflow = outflow,
                Expandable = expandable,
                Open = expanded.Contains(key),
                Icon = depth == 0 ? item.GetIcon() : null
            };
            VisibleRows.Add(row);
            return row;
        }

        void BuildBasic()
        {
            if (FoundationApi.Kingdom == null)
                return;
            var currentTotals = snapshot;
            // Section 1: CURRENCIES
            VisibleRows.Add(new GlobalLedgerRowData("basic/header/currencies", "CURRENCIES", 0) { IsHeader = true });
            int currencyCount = 0;
            foreach (GameCurrency currency in Enum.GetValues(typeof(GameCurrency)))
            {
                if (!currency.IsUnlocked())
                    continue;
                int c = (int)currency;
                bool hasActivity = book.HasCurrencyActivity(c);
                bool isActiveInWindow = currentTotals.Any(t => t.Active(c));
                double timeSinceActivity = book.TimeSinceCurrencyActivity(c);
                if (!showAll)
                {
                    if (currency != GameCurrency.Gold && !hasActivity)
                        continue;
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0 && currency != GameCurrency.Gold)
                        continue;
                }

                decimal net = GlobalLedgerMath.Sum(currentTotals.Where(t => t.Active(c)), t => t.Net(c));
                var row = new GlobalLedgerRowData("basic/currency/" + c, GlobalLedgerUi.CurrencyName(currency), 1)
                {
                    GameCurrency = currency,
                    Amount = net,
                    Income = GlobalLedgerMath.Sum(currentTotals, t => t.EarnedAt(c)),
                    Outflow = GlobalLedgerMath.Sum(currentTotals, t => t.SpentAt(c)),
                    Expandable = false,
                    Icon = currency.GetIcon()
                };
                VisibleRows.Add(row);
                currencyCount++;
            }

            if (currencyCount == 0 && !showAll)
            {
                VisibleRows.Add(new GlobalLedgerRowData("basic/currency/empty", "No observed currency activity in the recent window. Click Show All to list unlocked currencies.", 1));
            }

            // Section 2: PRODUCTION
            VisibleRows.Add(new GlobalLedgerRowData("basic/header/production", "ITEM PRODUCTION", 0) { IsHeader = true });
            var items = GlobalLedgerUi.OrderedUnlockedItems();
            int count = 0;
            foreach (var item in items)
            {
                string id = item.Identifier;
                bool hasActivity = book.HasItemActivity(id);
                bool isActiveInWindow = currentTotals.Any(t => t.ItemActive(id));
                double timeSinceActivity = book.TimeSinceItemActivity(id);
                if (!showAll)
                {
                    // 1. If something hasnt been made DO NOT SHOW IT
                    if (!hasActivity)
                        continue;
                    // 2. If somethings qty hasnt been greater than 0 within the selected time,
                    // and it has been more than a rolling minute (60s) since it was last made, DO NOT SHOW IT.
                    // 3. If something has been made, and value keeps changing: SHOW IT (isActiveInWindow == true).
                    // 4. If something has been made recently, but drops to 0: SHOW IT FOR A ROLLING MINUTE (timeSinceActivity <= windowSeconds + 60.0).
                    if (!isActiveInWindow && timeSinceActivity > windowSeconds + 60.0)
                        continue;
                }

                decimal net = GlobalLedgerMath.Sum(currentTotals.Where(t => t.ItemActive(id)), t => t.ItemNet(id));
                var row = new GlobalLedgerRowData("basic/item/" + id, GlobalLedgerUi.ItemName(item), 1)
                {
                    Item = item,
                    Amount = net,
                    Income = GlobalLedgerMath.Sum(currentTotals, t => t.ItemProduced(id)),
                    Outflow = GlobalLedgerMath.Sum(currentTotals, t => t.ItemConsumed(id)),
                    Expandable = false,
                    Icon = item.GetIcon()
                };
                VisibleRows.Add(row);
                count++;
            }

            if (count == 0 && !showAll)
            {
                VisibleRows.Add(new GlobalLedgerRowData("basic/item/empty", "No observed production in the recent window. Click Show All to list unlocked items.", 1));
            }
        }

        void BuildCurrencies()
        {
            if (FoundationApi.Kingdom == null)
                return;
            var data = locations.Resolve(snapshot);
            foreach (GameCurrency currency in Enum.GetValues(typeof(GameCurrency)))
            {
                if (!currency.IsUnlocked())
                    continue;
                int c = (int)currency;
                if (!showAll && !book.HasCurrencyActivity(c) && currency != GameCurrency.Gold) continue;
                string root = "currency/" + c;
                var active = data.Where(t => t.Totals.Active(c)).ToArray();
                Add(root, GlobalLedgerUi.CurrencyName(currency), 0, currency, GlobalLedgerMath.Sum(active, t => t.Totals.Net(c)), true, GlobalLedgerMath.Sum(active, t => t.Totals.EarnedAt(c)), GlobalLedgerMath.Sum(active, t => t.Totals.SpentAt(c)));
                if (!expanded.Contains(root) && string.IsNullOrEmpty(search))
                    continue;
                if (active.Length == 0)
                {
                    VisibleRows.Add(new GlobalLedgerRowData(root + "/empty", "No observed activity in the recent window.", 1));
                    continue;
                }

                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = Add(groupKey, container.GroupName, 1, currency, GlobalLedgerMath.Sum(group, t => t.Totals.Net(c)), true, GlobalLedgerMath.Sum(group, t => t.Totals.EarnedAt(c)), GlobalLedgerMath.Sum(group, t => t.Totals.SpentAt(c)));
                    containerRow.PrefixIcon = container.GroupTarget != null ? GlobalLedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon;
                    containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey) && string.IsNullOrEmpty(search))
                        continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = Add(buildingKey, entry.BuildingName, 2, currency, building.Net(c), true, building.EarnedAt(c), building.SpentAt(c));
                        buildingRow.Icon = entry.BuildingIcon;
                        buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey) && string.IsNullOrEmpty(search))
                            continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new GlobalLedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs) && string.IsNullOrEmpty(search))
                            continue;
                        bool any = false;
                        foreach (GameCurrency costCurrency in Enum.GetValues(typeof(GameCurrency)))
                        {
                            decimal spentCost = building.SpentAt((int)costCurrency);
                            if (spentCost != 0m && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/" + (int)costCurrency, GlobalLedgerUi.CurrencyName(costCurrency), 4, costCurrency, -spentCost, false, 0m, spentCost);
                                cost.Icon = costCurrency.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }
                        }

                        if (!any)
                            VisibleRows.Add(new GlobalLedgerRowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }
        }

        void BuildProduction()
        {
            if (FoundationApi.Kingdom == null)
                return;
            var data = locations.Resolve(snapshot);
            foreach (var item in GlobalLedgerUi.OrderedUnlockedItems())
            {
                string id = item.Identifier;
                string root = "item/" + id;
                var active = data.Where(t => t.Totals.ItemActive(id)).ToArray();
                if (!showAll && active.Length == 0)
                    continue;
                AddItem(root, GlobalLedgerUi.ItemName(item), 0, item, GlobalLedgerMath.Sum(active, t => t.Totals.ItemNet(id)), true, GlobalLedgerMath.Sum(active, t => t.Totals.ItemProduced(id)), GlobalLedgerMath.Sum(active, t => t.Totals.ItemConsumed(id)));
                if (!expanded.Contains(root) && string.IsNullOrEmpty(search))
                    continue;
                if (active.Length == 0)
                {
                    VisibleRows.Add(new GlobalLedgerRowData(root + "/empty", "No observed activity in the recent window.", 1));
                    continue;
                }

                foreach (var group in active.GroupBy(t => t.GroupKey).OrderBy(g => g.First().GroupName, StringComparer.CurrentCulture).ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    string groupKey = root + "/" + group.Key;
                    var container = group.First();
                    var containerRow = AddItem(groupKey, container.GroupName, 1, item, GlobalLedgerMath.Sum(group, t => t.Totals.ItemNet(id)), true, GlobalLedgerMath.Sum(group, t => t.Totals.ItemProduced(id)), GlobalLedgerMath.Sum(group, t => t.Totals.ItemConsumed(id)));
                    containerRow.PrefixIcon = container.GroupTarget != null ? GlobalLedgerLocations.ContainerIcon() : null;
                    containerRow.Icon = container.GroupIcon;
                    containerRow.Target = container.GroupTarget;
                    if (!expanded.Contains(groupKey) && string.IsNullOrEmpty(search))
                        continue;
                    foreach (var entry in group.OrderBy(t => t.BuildingName, StringComparer.CurrentCulture).ThenBy(t => t.Totals.Source.Id, StringComparer.Ordinal))
                    {
                        var building = entry.Totals;
                        string buildingKey = groupKey + "/" + building.Source.Id;
                        var buildingRow = AddItem(buildingKey, entry.BuildingName, 2, item, building.ItemNet(id), true, building.ItemProduced(id), building.ItemConsumed(id));
                        buildingRow.Icon = entry.BuildingIcon;
                        buildingRow.Target = entry.BuildingTarget;
                        if (!expanded.Contains(buildingKey) && string.IsNullOrEmpty(search))
                            continue;
                        string costs = buildingKey + "/costs";
                        VisibleRows.Add(new GlobalLedgerRowData(costs, "Operating Costs", 3) { Expandable = true, Open = expanded.Contains(costs) });
                        if (!expanded.Contains(costs) && string.IsNullOrEmpty(search))
                            continue;
                        bool any = false;
                        foreach (var costItem in GlobalLedgerUi.OrderedUnlockedItems())
                        {
                            decimal spentItem = building.ItemConsumed(costItem.Identifier);
                            if (spentItem != 0m)
                            {
                                var cost = AddItem(costs + "/item/" + costItem.Identifier, GlobalLedgerUi.ItemName(costItem), 4, costItem, -spentItem, false, 0m, spentItem);
                                cost.Icon = costItem.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }
                        }

                        foreach (GameCurrency costCurrency in Enum.GetValues(typeof(GameCurrency)))
                        {
                            decimal spentCost = building.SpentAt((int)costCurrency);
                            if (spentCost != 0m && costCurrency.IsUnlocked())
                            {
                                var cost = Add(costs + "/currency/" + (int)costCurrency, GlobalLedgerUi.CurrencyName(costCurrency), 4, costCurrency, -spentCost, false, 0m, spentCost);
                                cost.Icon = costCurrency.GetIcon();
                                cost.IconOnly = true;
                                any = true;
                            }
                        }

                        if (!any)
                            VisibleRows.Add(new GlobalLedgerRowData(costs + "/empty", "No observed operating costs", 4));
                    }
                }
            }

            if (VisibleRows.Count == 0)
                VisibleRows.Add(new GlobalLedgerRowData("production/empty", "No observed production in the selected window. Click Show All to list unlocked items.", 0));
        }

        public void ClosePopup() => ClosePopup(true);
        public void ClosePopup(bool playSound)
        {
            if (!gameObject.activeSelf)
                return;
            if (playSound)
                FoundationApi.UiAccess.PlaySound(GameUiSound.WindowClose);
            design?.CloseChoices();
            gameObject.SetActive(false);
            OnPopupClosed?.Invoke();
        }

        internal void CollapseAll()
        {
            expanded.Clear();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }

        internal static GlobalLedgerPanel Create(GlobalLedgerBook book, Button anchor)
        {
            // Foundation owns the bar slot. This popup has its own native popup lifecycle
            // outside the clipped slot and is destroyed with the Foundation view.
            var canvas = anchor.GetComponentInParent<Canvas>().rootCanvas;
            var root = GlobalLedgerUi.Rect(canvas.transform, "GlobalLedgerV2Dropdown");
            GlobalLedgerUi.Fill(root);
            root.gameObject.SetActive(false);
            var result = root.gameObject.AddComponent<GlobalLedgerPanel>();
            try
            {
                result.Initialize(book, anchor, root);
                return result;
            }
            catch
            {
                Destroy(root.gameObject);
                throw;
            }
        }

        public enum ViewMode
        {
            Basic,
            Adv
        }

        public enum TimeUnit
        {
            Sec,
            Min,
            Hour
        }

        public Action OnPopupClosed;
        GlobalLedgerBook book;
        GlobalLedgerUi ui;
        GlobalLedgerDesign design;
        Button anchor;
        RectTransform overlay, panel, content, viewport;
        TMP_Text status, footer;
        ScrollRect scroll;
        readonly HashSet<string> expanded = new HashSet<string>();
        readonly List<GlobalLedgerTreeRow> rows = new List<GlobalLedgerTreeRow>();
        readonly GlobalLedgerRowOrdering ordering = new GlobalLedgerRowOrdering();
        readonly GlobalLedgerSmoothing smoothing = new GlobalLedgerSmoothing();
        GlobalLedgerLocations locations => GlobalLedgerV2Plugin.Instance.Locations;
        internal readonly List<GlobalLedgerRowData> VisibleRows = new List<GlobalLedgerRowData>();
        IReadOnlyList<GlobalLedgerBookTotals> snapshot;
        readonly Vector3[] anchorCorners = new Vector3[4];
        float nextRefresh;
        double snapshotElapsed;
        int tab, sortField;
        bool showAll, descending, frozen, controlsCollapsed;
        string search = "", selectedKey = "basic/currency/0", lastLocale = "";
        ViewMode viewMode = ViewMode.Basic;
        TimeUnit timeUnit = TimeUnit.Min;
        static readonly int[][] WindowOptions = { new[] { 1, 10, 30 }, new[] { 1, 5, 10 }, new[] { 1, 12, 24 } };
        int windowIndex, refreshSeconds = 1;
        int UnitSeconds => timeUnit == TimeUnit.Sec ? 1 : timeUnit == TimeUnit.Hour ? 3600 : 60;
        int WindowAmount => WindowOptions[(int)timeUnit][windowIndex];
        int windowSeconds => WindowAmount * UnitSeconds;
        internal int SelectedTab => tab;
        internal bool ShowingAll => showAll;
        internal ViewMode Mode => viewMode;
        internal TimeUnit Unit => timeUnit;
        internal int WindowSeconds => windowSeconds;
        internal string StatusText => status != null ? status.text : "";
        internal RectTransform Frame => panel;
        internal ScrollRect Scroller => scroll;
        internal GlobalLedgerDesign Design => design;
        internal string SelectedKey => selectedKey;
        internal bool ControlsCollapsed => controlsCollapsed;
        float PanelHeight => GlobalLedgerDesign.Height - (controlsCollapsed ? GlobalLedgerDesign.ControlsHeight : 0);
        internal double Rate(decimal value) => GlobalLedgerMath.Rate(value, windowSeconds, UnitSeconds);
        internal decimal RateDecimal(decimal value) => GlobalLedgerMath.ScaleRate(value, windowSeconds, UnitSeconds);

        void Fit()
        {
            if (!panel) return;
            float fullWidth = overlay.rect.width, fullHeight = overlay.rect.height;
            ((RectTransform)anchor.transform).GetWorldCorners(anchorCorners);
            Vector2 buttonBottomRight = overlay.InverseTransformPoint(anchorCorners[3]);
            float top = Mathf.Max(8, overlay.rect.yMax - buttonBottomRight.y + 8);
            float scale = Mathf.Min(1, (fullWidth - 32) / GlobalLedgerDesign.Width, (fullHeight - top - 16) / PanelHeight);
            scale = Mathf.Max(.1f, scale);
            float x = Mathf.Clamp(buttonBottomRight.x - overlay.rect.xMin - (GlobalLedgerDesign.Width - 28) * scale, 16, Mathf.Max(16, fullWidth - GlobalLedgerDesign.Width * scale - 16));
            GlobalLedgerUi.Place(panel, x, top, GlobalLedgerDesign.Width, PanelHeight);
            panel.localScale = Vector3.one * scale;
        }

        void Initialize(GlobalLedgerBook book, Button anchor, RectTransform root)
        {
            this.book = book; this.anchor = anchor; overlay = root; ui = new GlobalLedgerUi(anchor);
            panel = GlobalLedgerUi.Rect(root, "GlobalLedgerV2Panel");
            panel.gameObject.AddComponent<FoundationUiInputBlocker>();
            design = new GlobalLedgerDesign(ui, panel);
            // The panel face consumes clicks so they do not reach the factory underneath.
            design.Node("Main panel").GetComponent<Image>().raycastTarget = true;
            BindControls();
            CreateTable();
            status = design.Label("Observation window"); footer = design.Label("Row count");
            Fit();
        }

        void BindControls()
        {
            design.StyleOverviewButton(design.Click("Overview heading", ToggleControls));
            design.Click("Close ledger", ClosePopup);
            design.Click("Rate window 1", () => SetWindow(0));
            design.Click("Rate window 2", () => SetWindow(1));
            design.Click("Rate window 3", () => SetWindow(2));
            design.Click("Basic mode", () => SetViewMode(ViewMode.Basic));
            design.Click("Advanced mode", () => SetViewMode(ViewMode.Adv));
            design.Click("Rolling calculation toggle", ToggleRollingAverage);
            design.Click("Rate units/Decrease", () => CycleUnit(-1));
            design.Click("Rate units/Increase", () => CycleUnit(1));
            design.Click("Sort field", () => design.Choices("Sort field", GlobalLedgerRowOrdering.Fields, value => { sortField = value; RefreshNow(); }));
            design.Click("Sort direction", () => design.Choices("Sort direction", new[] { "Ascending", "Descending" }, value => { descending = value == 1; RefreshNow(); }));
            design.Click("Refresh interval/Decrease", () => SetRefresh(-1));
            design.Click("Refresh interval/Increase", () => SetRefresh(1));
            design.Click("Freeze toggle", () => { frozen = !frozen; if (!frozen) nextRefresh = 0; RefreshNow(); });
            design.Click("Show all checkbox", ToggleShowAll);
            design.Click("Show all label", ToggleShowAll);
            design.Click("Currencies tab", () => SelectTab(0));
            design.Click("Production tab", () => SelectTab(1));
            design.Click("Collapse rows", CollapseAll);
            design.Search(value => { search = value; scroll.StopMovement(); content.anchoredPosition = Vector2.zero; RefreshNow(); });
        }

        void CreateTable()
        {
            viewport = GlobalLedgerUi.Rect(panel, "TreeViewport"); GlobalLedgerUi.Place(viewport, 40, 322, GlobalLedgerDesign.RowWidth, 392);
            ui.Background(viewport, Color.clear); viewport.gameObject.AddComponent<RectMask2D>();
            content = GlobalLedgerUi.Rect(viewport, "TreeContent");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0, 1); content.sizeDelta = Vector2.zero;
            scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 48;
            var track = GlobalLedgerUi.Rect(panel, "ScrollTrack"); GlobalLedgerUi.Place(track, 776, 322, 4, 384);
            ui.Background(track, GlobalLedgerDesign.Color("#1e211f"));
            var handle = GlobalLedgerUi.Rect(track, "ScrollHandle"); GlobalLedgerUi.Fill(handle);
            var ink = ui.Background(handle, GlobalLedgerDesign.Color("#bda06b"));
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle; bar.targetGraphic = ink;
            bar.direction = Scrollbar.Direction.BottomToTop; bar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        void CycleUnit(int direction) => SetTimeUnit((TimeUnit)(((int)timeUnit + direction + 3) % 3));
        void ToggleControls()
        {
            controlsCollapsed = !controlsCollapsed;
            design.CloseChoices();
            design.SetControlsCollapsed(controlsCollapsed);
            float shift = controlsCollapsed ? GlobalLedgerDesign.ControlsHeight : 0;
            GlobalLedgerUi.Place(viewport, 40, 322 - shift, GlobalLedgerDesign.RowWidth, 392);
            GlobalLedgerUi.Place((RectTransform)scroll.verticalScrollbar.transform, 776, 322 - shift, 4, 384);
            Fit();
        }
        void SetWindow(int index) { windowIndex = index; InvalidateWindow(); }
        void InvalidateWindow() { smoothing.Clear(); snapshot = null; RefreshNow(); }
        void SetRefresh(int change) { refreshSeconds = Mathf.Clamp(refreshSeconds + change, 1, 10); nextRefresh = Time.unscaledTime + refreshSeconds; RefreshNow(); }
        void OnDestroy() => design?.Dispose();

        void OnEnable()
        {
            if (book != null)
            {
                transform.SetAsLastSibling();
                Fit();
                RefreshNow();
            }
        }

        public void OpenPopup()
        {
            if (gameObject.activeSelf)
                return;
            nextRefresh = 0;
            FoundationApi.UiAccess.PlaySound(GameUiSound.WindowOpen);
            gameObject.SetActive(true);
            RefreshNow();
        }

        internal void RefreshNow()
        {
            string locale = GlobalLedgerLocations.CurrentLocale;
            if (!string.Equals(locale, lastLocale, StringComparison.Ordinal))
            {
                lastLocale = locale;
                locations.Reset();
                smoothing.Clear();
                snapshot = null;
            }
            bool sample = snapshot == null || (!frozen && Time.unscaledTime >= nextRefresh);
            if (sample)
            {
                snapshot = book.Snapshot(windowSeconds);
                snapshotElapsed = book.Elapsed;
                nextRefresh = Time.unscaledTime + refreshSeconds;
            }
            UpdateControls();
            VisibleRows.Clear();
            if (viewMode == ViewMode.Basic) BuildBasic();
            else if (tab == 0) BuildCurrencies();
            else BuildProduction();
            bool rolling = GlobalLedgerV2Plugin.Instance?.EnableRollingAverage?.Value == true;
            int entries = Mathf.Clamp(GlobalLedgerV2Plugin.Instance?.RollingAverageEntries?.Value ?? 3, 2, 60);
            if (rolling) smoothing.Apply(VisibleRows, entries, sample);
            var ordered = ordering.Arrange(VisibleRows, search, sortField, descending);
            VisibleRows.Clear(); VisibleRows.AddRange(ordered);
            float top = 0;
            for (int i = 0; i < VisibleRows.Count; i++)
            {
                var row = VisibleRows[i]; row.Top = top; top += row.IsHeader ? 28 : 48;
                while (rows.Count <= i) rows.Add(new GlobalLedgerTreeRow(this, ui, content, rows.Count));
                rows[i].Bind(row, GlobalLedgerDesign.RowWidth);
            }
            for (int i = VisibleRows.Count; i < rows.Count; i++) rows[i].Root.gameObject.SetActive(false);
            content.sizeDelta = new Vector2(0, top);
            int count = VisibleRows.Count(value => !value.IsHeader && (value.GameCurrency.HasValue || value.Item != null));
            footer.text = count + (viewMode == ViewMode.Basic ? " active assets" : " sources");
            string unitWord = timeUnit == TimeUnit.Sec ? "second" : timeUnit == TimeUnit.Hour ? "hour" : "minute";
            string collecting = snapshotElapsed < windowSeconds ? "  \u00b7  Collecting " + Math.Max(0, (int)snapshotElapsed) + "/" + windowSeconds + "s" : "";
            status.text = "Last " + WindowAmount + " " + unitWord + (WindowAmount == 1 ? "" : "s") + "  ·  Per-" + unitWord + " rates" + (frozen ? "  ·  Frozen" : "") + collecting + (count == 0 && search.Length != 0 ? "  ·  No matching assets" : "");
        }

        void UpdateControls()
        {
            bool advanced = viewMode == ViewMode.Adv;
            design.Node("Advanced toolbar").gameObject.SetActive(advanced);
            design.Node("Table title").gameObject.SetActive(!advanced);
            design.Label("Overview heading").text = advanced ? tab == 0 ? "CURRENCY BREAKDOWN" : "PRODUCTION BREAKDOWN" : "KINGDOM OVERVIEW";
            design.Label("Live update status").text = (frozen ? "FROZEN" : "LIVE") + "  ·  Refresh " + refreshSeconds + "s";
            design.Label("Refresh interval/Value").text = refreshSeconds.ToString();
            design.Label("Rate units/Value").text = "Per " + (timeUnit == TimeUnit.Sec ? "second" : timeUnit == TimeUnit.Hour ? "hour" : "minute");
            design.Label("Sort field/Selected value").text = GlobalLedgerRowOrdering.Fields[sortField];
            design.Label("Sort direction/Selected value").text = descending ? "Descending" : "Ascending";
            string windowSuffix = timeUnit == TimeUnit.Sec ? "s" : timeUnit == TimeUnit.Hour ? "h" : "m";
            for (int i = 0; i < 3; i++)
            {
                string name = "Rate window " + (i + 1);
                design.Radio(name, windowIndex == i);
                var label = design.Label(name + "/Option label");
                label.text = WindowOptions[(int)timeUnit][i] + windowSuffix;
                label.color = windowIndex == i ? GlobalLedgerUi.Paper : GlobalLedgerUi.Muted;
            }
            design.Radio("Basic mode", !advanced); design.Radio("Advanced mode", advanced);
            design.Label("Basic mode/Option label").color = advanced ? GlobalLedgerUi.Muted : GlobalLedgerUi.Paper;
            design.Label("Advanced mode/Option label").color = advanced ? GlobalLedgerUi.Paper : GlobalLedgerUi.Muted;
            design.Radio("Rolling calculation toggle", GlobalLedgerV2Plugin.Instance?.EnableRollingAverage?.Value == true);
            design.Radio("Freeze toggle", frozen);
            design.Node("Show all checkbox").GetComponent<Image>().sprite = design.Asset(showAll ? "Checkbox_1" : "Checkbox_0");
            design.Label("Rolling average label").text = "Avg: " + (GlobalLedgerV2Plugin.Instance?.RollingAverageEntries?.Value ?? 3) + " samples";
            string suffix = timeUnit.ToString().ToUpperInvariant();
            design.Label("Income heading").text = "IN / " + suffix; design.Label("Outflow heading").text = "OUT / " + suffix; design.Label("Net heading").text = "NET / " + suffix;
            foreach (string name in new[] { "Currencies tab", "Production tab" })
                design.Node(name + "/Button frame").GetComponent<Image>().sprite = design.Asset((name == "Currencies tab" ? tab == 0 : tab == 1) ? "Button02" : "Button03");
        }

        internal void ResetTree()
        {
            expanded.Clear();
            snapshot = null; snapshotElapsed = 0; nextRefresh = 0; smoothing.Clear(); frozen = false;
            showAll = false;
            locations.Reset();
            scroll.verticalNormalizedPosition = 1;
        }

        internal void SelectTab(int value)
        {
            tab = value;
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }

        internal void SetTimeUnit(TimeUnit unit)
        {
            if (timeUnit == unit)
                return;
            timeUnit = unit;
            InvalidateWindow();
        }

        internal void SetViewMode(ViewMode mode)
        {
            if (viewMode == mode)
                return;
            viewMode = mode;
            selectedKey = mode == ViewMode.Basic ? "basic/currency/0" : tab == 0 ? "currency/0" : "";
            Fit();
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshNow();
        }

        public void TogglePopup()
        {
            if (gameObject.activeSelf)
                ClosePopup();
            else
                OpenPopup();
        }

        internal void ToggleRollingAverage()
        {
            smoothing.Clear();
            if (GlobalLedgerV2Plugin.Instance?.EnableRollingAverage != null)
            {
                GlobalLedgerV2Plugin.Instance.EnableRollingAverage.Value = !GlobalLedgerV2Plugin.Instance.EnableRollingAverage.Value;
            }

            RefreshNow();
        }

        internal void ToggleRow(string key)
        {
            if (!expanded.Add(key))
                expanded.Remove(key);
            RefreshNow();
        }

        internal void ToggleShowAll()
        {
            showAll = !showAll;
            RefreshNow();
        }

        void Update()
        {
            var bar = anchor ? anchor.transform.parent?.parent?.parent as RectTransform : null;
            if (!anchor || !anchor.gameObject.activeInHierarchy || !anchor.GetComponentInParent<Canvas>().enabled || (bar && bar.anchoredPosition.y > 0.5f))
            {
                if (FoundationApi.UiAccess.Root)
                    ClosePopup();
                return;
            }

            if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();
            Fit();
            if (!frozen && Time.unscaledTime >= nextRefresh)
                RefreshNow();
        }

        internal void View(GlobalLedgerRowData row)
        {
            if (row?.Target != null)
                GlobalLedgerV2Plugin.Instance.Navigation.Focus(row.Target);
        }
    }
}
