using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Puch.FirebirdHelper
{
    [AttributeUsage(AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute([CallerMemberName] string name = null)
        {
            this.Name = name.ToUpperInvariant();
        }
        public string Name { get; private set; }
    }
}
