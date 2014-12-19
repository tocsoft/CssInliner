using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner.Tests
{
    public static class ObjectExtensions
    {
        public static void SetProperty(this object obj, string propertyName, string value)
        {
            var p = obj.GetType().GetProperties().FirstOrDefault(x => x.Name.ToUpper() == propertyName.Trim().ToUpper());
            if (p != null)
            {
                if (p.PropertyType == typeof(bool))
                {
                    p.SetMethod.Invoke(obj, new object[] { bool.Parse(value) });
                }
                else if (p.PropertyType == typeof(string))
                {
                    p.SetMethod.Invoke(obj, new object[] { value.Trim() });
                }

            }
        }

    }
}
