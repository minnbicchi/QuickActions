using System.Collections;
using UnityEngine;

namespace MinnBicchi.SupportedPlugins
{
    public class SupportedPlugin
    {
        protected JSONStorable _storable;
        protected QuickActions _script;
        protected string _parentUid;

        public SupportedPlugin(JSONStorable storable, string parentUid, QuickActions script)
        {
            _storable = storable;
            _script = script;
            _parentUid = parentUid;
        }

        public string Name()
        {
            return _storable.name + " QuickActions";
        }

        public virtual IEnumerator InitActions(JSONStorable parentStorable)
        {
            yield return new WaitForEndOfFrame();
        }
    }
}