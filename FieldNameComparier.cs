using System.Collections.Generic;

namespace System.Data.SqlClient
{
    internal class FieldNameComparier : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLowerInvariant().GetHashCode();
        }
    }
}