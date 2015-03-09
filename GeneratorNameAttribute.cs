using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puch.FirebirdHelper
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GeneratorNameAttribute : Attribute
    {
        public GeneratorNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

    }
}
