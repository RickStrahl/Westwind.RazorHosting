using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Westwind.RazorHosting
{

    /// <summary>
    /// This class handles special non-public types - anonymous types
    /// and allows returning property values from them.
    ///
    /// Requires Reflection permissions for internal,private properties
    /// </summary>
    public class AnonymousDynamicType : DynamicObject
    {
        /// <summary>
        /// Internally stored instance on which to look up properties
        /// via Reflection
        /// </summary>
        object Instance = null;

        /// <summary>
        /// Require passing in off an instance
        /// </summary>
        /// <param name="instance"></param>
        public AnonymousDynamicType(object instance)
        {
            Instance = instance;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return GetProperty(Instance, binder.Name, out result);
        }

        private bool GetProperty(object instance, string name, out object result)
        {
            if (instance == null)
                instance = this;

            var miArray = instance.GetType().GetMember(name, BindingFlags.Public | BindingFlags.NonPublic |
                                                             BindingFlags.GetProperty | BindingFlags.Instance);
            if (miArray != null && miArray.Length > 0)
            {
                var mi = miArray[0];
                if (mi.MemberType == MemberTypes.Property)
                {
                    result = ((PropertyInfo)mi).GetValue(instance, null);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
