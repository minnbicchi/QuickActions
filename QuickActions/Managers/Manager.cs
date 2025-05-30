using System.Collections.Generic;
namespace MinnBicchi.Managers
{
    using SupportedPlugins;
    public class Manager<T> where T : SupportedPlugin
    {
        private List<T> _plugins = new List<T>();
        private JSONStorableString _parentUid;
        private Atom _parent;
        private QuickActions _script;
        private JSONStorable _jsonStorable;
        
        public Manager(Atom parent, QuickActions script)
        {
            _parentUid = new JSONStorableString("_managerParentUid", parent.uid);
            _parent = parent;
            _script = script;
        }

        public void AddPlugin(T plugin)
        {
            _plugins.Add(plugin);
            JSONStorable js;
            if (_parent.GetStorableIDs().Contains(plugin.Name()))
            {
                js = _parent.GetStorableByID(plugin.Name());
            }
            else
            {
                js = _parent.gameObject.AddComponent<JSONStorable>();
                js.name = plugin.Name();
                _parent.RegisterAdditionalStorable(js);
            }
            _jsonStorable = js;
            SuperController.singleton.StartCoroutine(plugin.InitActions(_jsonStorable));
        }
    }
}