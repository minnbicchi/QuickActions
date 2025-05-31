using System.Diagnostics;
using System.Collections.Generic;
using MinnBicchi.Managers;

namespace MinnBicchi.SupportedPlugins
{
    public class SupportedPluginManager
    {
        List<string> supportedPlugins = new List<string>
        {
            "VamEssentials.DockedUI",
            "Embody",
            "ToumeiHitsuji.DivingRod",
            "Vinput.AutoThruster",
            "VamTimeline.AtomPlugin"
        };
        
        private Dictionary<string, Manager<DockedUIPlugin, DockedUIPluginActions>> _managedDockedUIPlugins = 
            new Dictionary<string, Manager<DockedUIPlugin,DockedUIPluginActions>>();
        private QuickActions _script;

        public SupportedPluginManager(QuickActions script)
        {
            _script = script;
        }

        public void ProcessAtom(Atom parent, bool filterParent)
        {
            parent.GetStorableIDs().ForEach(jsonStoreID =>
            {
                supportedPlugins.ForEach(s =>
                {
                    if (jsonStoreID.EndsWith(s))
                    {
                        switch (s)
                        {
                            case "VamEssentials.DockedUI":
                                if (!_managedDockedUIPlugins.ContainsKey(parent.uid))
                                {
                                    _managedDockedUIPlugins[parent.uid] = 
                                        new Manager<DockedUIPlugin, DockedUIPluginActions>(parent, _script);
                                }
                                _managedDockedUIPlugins[parent.uid].AddPlugin(
                                    new DockedUIPlugin(parent.GetStorableByID(jsonStoreID), parent.uid, _script)
                                    );
                                SuperController.LogMessage("Found managed plugin: " + jsonStoreID + "on: " + parent.uid);
                                break;
                        }
                    }
                }); 
            });
        }
    }
}