using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace XSerializer
{
    static class Tools
    {
        public static CultureInfo GetCulture(this ISerializeOptions options)
            => options?.Culture ?? CultureInfo.InvariantCulture;
    }
}
