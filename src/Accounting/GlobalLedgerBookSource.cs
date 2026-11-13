using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerBookSource
    {
        internal GlobalLedgerBookSource(string id, string typeId, string typeName, string name)
        {
            Id = id;
            TypeId = typeId;
            TypeName = typeName;
            Name = name;
        }

        internal readonly string Id, TypeId, TypeName, Name;
    }
}
