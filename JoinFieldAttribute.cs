using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puch.FirebirdHelper
{
    [AttributeUsage(AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
    public class JoinFieldAttribute : Attribute
    {
    }
}
