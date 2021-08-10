using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace MDTNamer
{
    public static class Security
    {
        public static bool IsInGroup(this ClaimsPrincipal User, string GroupName)
        {
            var groups = new List<string>();

            var wi = (WindowsIdentity)User.Identity;
            if (wi.Groups != null)
            {
                foreach (var group in wi.Groups)
                {
                    try
                    {
                        groups.Add(group.Translate(typeof(NTAccount)).ToString());
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                return groups.Contains(GroupName);
            }
            return false;
        }
    }
}
