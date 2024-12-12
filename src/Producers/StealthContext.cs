using System.Collections.Generic;
using SharpHoundCommonLib;

namespace Sharphound.Producers
{
    public static class StealthContext
    {
        private static Dictionary<string, IDirectoryObject> _stealthTargetSids;


        internal static void AddStealthTargetSids(Dictionary<string, IDirectoryObject> targets)
        {
            if (_stealthTargetSids == null)
                _stealthTargetSids = targets;
            else
                foreach (var target in targets)
                    _stealthTargetSids.Add(target.Key, target.Value);
        }

        internal static bool IsSidStealthTarget(string sid)
        {
            return _stealthTargetSids.ContainsKey(sid);
        }

        internal static IEnumerable<IDirectoryObject> GetSearchResultEntries()
        {
            return _stealthTargetSids.Values;
        }
    }
}