using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomGlobalLedgerV2.Verification
{
    internal sealed class GlobalLedgerBaselineAdapter
    {
        readonly Assembly assembly;
        readonly object plugin;
        readonly Type totalsType, sourceType, locationsType;
        internal readonly Func<WorkspaceObject, object> Begin;
        internal readonly Action<object> End;

        internal GlobalLedgerBaselineAdapter()
        {
            plugin = Chainloader.PluginInfos["urakka.kingdominc.globalledger"].Instance;
            assembly = plugin.GetType().Assembly;
            totalsType = assembly.GetType("KingdomGlobalLedger.LedgerBookTotals");
            sourceType = assembly.GetType("KingdomGlobalLedger.LedgerBookSource");
            locationsType = assembly.GetType("KingdomGlobalLedger.LedgerLocations");
            var observer = assembly.GetType("KingdomGlobalLedger.LedgerObserver");
            var element = Expression.Parameter(typeof(WorkspaceObject));
            Begin = Expression.Lambda<Func<WorkspaceObject, object>>(Expression.Convert(
                Expression.Call(AccessTools.Method(observer, "Begin"), element), typeof(object)), element).Compile();
            var state = Expression.Parameter(typeof(object));
            var end = AccessTools.Method(observer, "End");
            End = Expression.Lambda<Action<object>>(Expression.Call(end,
                Expression.Convert(state, end.GetParameters()[0].ParameterType)), state).Compile();
        }

        internal object Book => AccessTools.Field(plugin.GetType(), "Book").GetValue(plugin);
        internal void Reset() => AccessTools.Method(Book.GetType(), "Reset").Invoke(Book, null);

        internal Dictionary<string, string> Snapshot()
        {
            var result = new Dictionary<string, string>();
            var values = (IEnumerable)AccessTools.Method(Book.GetType(), "Snapshot").Invoke(Book, new object[] { 60 });
            foreach (var totals in values)
            {
                var source = Field(totals, "Source");
                var earned = (long[])Field(totals, "Earned");
                var spent = (long[])Field(totals, "Spent");
                var items = (Dictionary<string, long>)Field(totals, "ItemEarned");
                var consumed = (Dictionary<string, long>)Field(totals, "ItemSpent");
                result[(string)Field(source, "Id")] = Encode(earned, spent, items, consumed);
            }
            return result;
        }

        internal static string Encode(long[] earned, long[] spent, Dictionary<string, long> items, Dictionary<string, long> consumed)
            => string.Join(",", earned) + "/" + string.Join(",", spent) + "/"
                + string.Join(",", items.OrderBy(pair => pair.Key).Select(pair => pair.Key + ":" + pair.Value)) + "/"
                + string.Join(",", consumed.OrderBy(pair => pair.Key).Select(pair => pair.Key + ":" + pair.Value));

        internal object NewLocations() => Activator.CreateInstance(locationsType, true);

        internal object LocationTotals(IEnumerable<GlobalLedgerBookTotals> totals)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(totalsType));
            foreach (var total in totals)
            {
                var source = Activator.CreateInstance(sourceType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { total.Source.Id, total.Source.TypeId, total.Source.TypeName, total.Source.Name }, null);
                list.Add(Activator.CreateInstance(totalsType, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { source }, null));
            }
            return list;
        }

        internal IEnumerable<object> Resolve(object locations, object totals)
            => ((IEnumerable)AccessTools.Method(locationsType, "Resolve").Invoke(locations, new[] { totals })).Cast<object>();

        internal static object Field(object value, string name) => AccessTools.Field(value.GetType(), name).GetValue(value);
    }
}
