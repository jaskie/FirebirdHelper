using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puch.FirebirdHelper
{
    [AttributeUsage(AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
    public class DatabaseAutoUpdatedAttribute : Attribute
    {
        public DatabaseAutoUpdatedAttribute(bool onInsert = true, bool onUpdate = true )
        {
            OnInsert = onInsert;
            OnUpdate = onUpdate;
        }
        public bool OnInsert { get; private set; }
        public bool OnUpdate { get; private set; }
    }
}
