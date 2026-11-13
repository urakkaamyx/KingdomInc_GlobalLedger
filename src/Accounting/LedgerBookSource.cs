using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerBookSource
    {
        internal LedgerBookSource(string id, string typeId, string typeName, string name)
        {
            Id = id;
            TypeId = typeId;
            TypeName = typeName;
            Name = name;
        }

        internal readonly string Id, TypeId, TypeName, Name;
    }
}
