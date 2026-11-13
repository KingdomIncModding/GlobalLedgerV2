using KingdomFoundation.Api;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Linq;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerNavigation : MonoBehaviour
    {
        internal void Clear()
        {
            hover = selected = null;
            Hide();
        }

        GlobalLedgerTarget hover, selected, shown;
        GlobalLedgerTargetOutline outline;
        float selectedUntil;
        internal GlobalLedgerTarget Highlighted => shown;

        static WorkspaceObject FindMacroForFlow(GameWorkspace targetFlow)
        {
            if (targetFlow == null || FoundationApi.Kingdom?.Workspaces == null)
                return null;
            foreach (var rootFlow in FoundationApi.Kingdom.Workspaces)
            {
                var found = FindMacroRecursive(rootFlow, targetFlow);
                if (found != null)
                    return found;
            }

            return null;
        }

        static WorkspaceObject FindMacroRecursive(GameWorkspace currentFlow, GameWorkspace targetFlow)
        {
            if (currentFlow?.Buildings == null)
                return null;
            for (int i = 0; i < currentFlow.Buildings.Count; i++)
            {
                var el = currentFlow.Buildings[i];
                if (el == null)
                    continue;
                var inner = FoundationApi.WorkspaceAccess.ExtensionWorkspace(el, "InnerFlow");
                if (inner != null)
                {
                    if (inner == targetFlow)
                        return el;
                    var deeper = FindMacroRecursive(inner, targetFlow);
                    if (deeper != null)
                        return deeper;
                }
            }

            return null;
        }

        internal bool Focus(GlobalLedgerTarget target)
        {
            if (target == null || !target.Exists || !FoundationApi.WorkspaceAccess.Root || !FoundationApi.UiAccess.Root)
                return false;
            if (FoundationApi.UiAccess.IsMenuOpen(GameMenu.Research))
                FoundationApi.UiAccess.SetMenuVisible(GameMenu.Research, false);
            int index = FoundationApi.Kingdom.Workspaces.ToList().IndexOf(target.Flow);
            if (index >= 0)
            {
                FoundationApi.World.SelectFlow(index);
            }
            else
            {
                var parentMacro = FindMacroForFlow(target.Flow);
                if (parentMacro != null)
                {
                    var mdmType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("KingdomLeylineLogic.MacroDimensionManager", false)).FirstOrDefault(type => type != null);
                    var enterMethod = mdmType != null ? mdmType.GetMethod("EnterMacroDimension", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                    if (enterMethod != null)
                        FoundationApi.WorkspaceAccess.InvokeExtension(enterMethod, parentMacro);
                }
            }

            if (FoundationApi.CameraAccess.ActiveView)
                FoundationApi.View.RecalculateCameraBounds();
            var view = target.View;
            if (!view || !Camera.main)
                return false;
            var center = view.TransformPoint(view.rect.center);
            var camera = Camera.main;
            camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
            target.Flow.CameraPosition = new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
            selected = target;
            selectedUntil = Time.unscaledTime + 5;
            hover = null;
            Refresh();
            return true;
        }

        void Hide()
        {
            if (outline)
            {
                outline.gameObject.SetActive(false);
                Destroy(outline.gameObject);
            }

            outline = null;
            shown = null;
        }

        internal void Hover(GlobalLedgerTarget target)
        {
            hover = target;
            Refresh();
        }

        void LateUpdate() => Refresh();
        void OnDestroy() => Clear();
        void Refresh()
        {
            if (!FoundationApi.UiAccess.Root || !FoundationApi.UiAccess.Root.GetComponent<Canvas>().enabled)
            {
                Clear();
                return;
            }

            if (selected != null && (Time.unscaledTime >= selectedUntil || !selected.Exists))
                selected = null;
            if (hover != null && !hover.Exists)
                hover = null;
            var target = hover ?? selected;
            var view = target?.View;
            if (target == null || !view || !view.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            if (!GlobalLedgerTarget.Same(shown, target) || !outline)
            {
                Hide();
                shown = target;
                outline = GlobalLedgerTargetOutline.Create(view);
            }

            outline.SetWidth(FoundationApi.CameraAccess.ActiveView ? 10 / Mathf.Max(.1f, FoundationApi.CameraAccess.ZoomScale(FoundationApi.CameraAccess.ActiveView)) : 5);
        }

        internal void Unhover(GlobalLedgerTarget target)
        {
            if (GlobalLedgerTarget.Same(hover, target))
            {
                hover = null;
                Refresh();
            }
        }
    }
}
