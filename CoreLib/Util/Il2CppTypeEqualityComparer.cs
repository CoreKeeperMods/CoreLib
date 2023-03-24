using System.Collections.Generic;
using Il2CppSystem;

namespace CoreLib.Util
{
    public class Il2CppTypeEqualityComparer : EqualityComparer<Type>
    {
        public override bool Equals(Type x, Type y)
        {
            if (x == null)
                return y == null;
            if (y == null)
                return false;

            return x.FullName.Equals(y.FullName);
        }

        public override int GetHashCode(Type obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}