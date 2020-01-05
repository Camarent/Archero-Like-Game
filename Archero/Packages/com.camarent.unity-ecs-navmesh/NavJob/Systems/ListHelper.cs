using System.Collections.Generic;
using System.Text;

namespace NavJob.Systems
{
    public static class ListHelper
    {
        public static string SerializedView<T>(this List<T> list)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            foreach (var element in list)
            {
                builder.Append(element);
                builder.Append(",");
            }
            builder.Append("]");
            return builder.ToString();
        }

    }
}