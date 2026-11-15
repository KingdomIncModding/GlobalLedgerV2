using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace KingdomGlobalLedgerV2
{
    internal static class GlobalLedgerCustomNodeHooks
    {
        sealed class CompositeHook : IDisposable
        {
            public void Dispose() => Clear();
        }

        static readonly Dictionary<Type, IDisposable> subscribed = new Dictionary<Type, IDisposable>();
        static readonly Type[] UpdateParams = { typeof(float) };
        static bool active;

        internal static void Install(List<IDisposable> hooks)
        {
            Clear();
            active = true;
            ScanLoadedAssemblies();
            hooks.Add(new CompositeHook());
            hooks.Add(FoundationApi.Events.Subscribe<WorkspaceChangedEvent>(GlobalLedgerV2Plugin.Id, change => ObserveActiveFlows()));
            if (FoundationApi.WorkspaceNodes != null)
            {
                Action changed = ScanLoadedAssemblies;
                FoundationApi.WorkspaceNodes.Changed += changed;
                hooks.Add(new Subscription(() => FoundationApi.WorkspaceNodes.Changed -= changed));
            }
        }

        sealed class Subscription : IDisposable
        {
            readonly Action dispose;
            internal Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose() => dispose();
        }

        internal static void ObserveBuilding(WorkspaceObject element)
        {
            if (!active || element?.Behaviour == null)
                return;
            SubscribeType(element.Behaviour.GetType());
        }

        static void ObserveActiveFlows()
        {
            if (!active || FoundationApi.Kingdom == null)
                return;
            foreach (var flow in GlobalLedgerLocations.GetAllFlows(FoundationApi.Kingdom))
                foreach (var building in flow.Buildings)
                    ObserveBuilding(building);
        }

        internal static void Clear()
        {
            active = false;
            foreach (var hook in subscribed.Values)
                hook?.Dispose();
            subscribed.Clear();
        }

        static void ScanLoadedAssemblies()
        {
            if (!active)
                return;
            string foundationName = typeof(FrameworkWorkspaceNode).Assembly.GetName().Name;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;
                string name = assembly.GetName().Name;
                if (name == foundationName)
                    continue;
                bool referencesFoundation = false;
                try
                {
                    foreach (var reference in assembly.GetReferencedAssemblies())
                    {
                        if (reference.Name == foundationName)
                        {
                            referencesFoundation = true;
                            break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
                if (!referencesFoundation)
                    continue;
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }
                if (types == null)
                    continue;
                for (int i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (type != null && !type.IsAbstract && type.IsSubclassOf(typeof(FrameworkWorkspaceNode)))
                        SubscribeType(type);
                }
            }
        }

        internal static bool SubscribeType(Type nodeType)
        {
            if (!active || nodeType == null || nodeType.IsAbstract || !nodeType.IsSubclassOf(typeof(FrameworkWorkspaceNode)))
                return false;
            if (subscribed.ContainsKey(nodeType))
                return true;
            var update = nodeType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, UpdateParams, null);
            if (update == null || update.DeclaringType == typeof(FrameworkWorkspaceNode))
            {
                subscribed[nodeType] = null;
                return false;
            }
            var hook = FoundationApi.ExecutionHooks.SubscribeExternal(
                GlobalLedgerV2Plugin.Id,
                update,
                call => GlobalLedgerObserver.Begin((call.Subject as FrameworkWorkspaceNode)?.Model),
                (call, state) => GlobalLedgerObserver.Leave(state as GlobalLedgerFrame, call.Exception));
            subscribed[nodeType] = hook;
            return hook != null;
        }
    }
}

