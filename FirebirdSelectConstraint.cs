using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puch.FirebirdHelper
{
    public class FirebirdSelectConstraint<T> where T : FirebirdRow
    {
        [Flags]
        public enum ConditionType
        {
            Equal = 0x1,
            Greather = 0x2,
            Smaller = 0x4,
            NotEqal = Greather | Smaller,
            GreatherOrEqual = Greather | Equal,
            SmallerOrEqal = Smaller | Equal,
        }
        public ConditionType Condition { get; private set; }
        public string FieldName { get; private set; }
        public object Value { get; private set; }
        public FirebirdSelectConstraint(ConditionType condition, object value)
        {
            Condition = condition;
            Value = value;
        }
    }
}
