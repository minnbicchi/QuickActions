using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;


namespace MinnBicchi
{
    public class QuickActions : MVRScript
    {
        private static List<string> ClothingOptions = new List<string>
        {
            "Toggle",
            "Undress",
            "On",
            "Off",
            "ResetSim"
        };

        public abstract class Action
        {
            private string _personAtomUid;
            private JSONStorableStringChooser _stringChooser;
            private JSONStorableActionStringChooser _jsonStorableActionStringChooser;

            protected Action(string paramName, List<string> choicesList, string startingValue, string displayName,
                string personAtomUid)
            {
                _stringChooser =
                    new JSONStorableStringChooser(paramName + "_chooser", choicesList, startingValue, displayName);
                _jsonStorableActionStringChooser =
                    new JSONStorableActionStringChooser(paramName, DoAction, _stringChooser);
            }

            public abstract void DoAction(string val);
        }

        private class ClothingAction : Action
        {
            private string _itemname;
            private MVRScript _script;
            private string _personAtomUID;
            private List<string> _dockedUIStoreIDs;

            public ClothingAction(string paramName, string personAtomUid, MVRScript script, string itemname) :
                base(paramName, ClothingOptions.ToList(), ClothingOptions[0], paramName, personAtomUid)
            {
                _itemname = itemname;
                _script = script;
                _personAtomUID = personAtomUid;
            }

            public override void DoAction(string val)
            {
                Atom person = _script.GetAtomById(_personAtomUID);
                switch (val)
                {
                    case "On":
                    case "Off":
                        foreach (string storableID in person.GetStorableIDs())
                        {
                            if (storableID == "geometry")
                            {
                                var geometry = person.GetStorableByID(storableID);
                                var item = geometry.GetBoolJSONParam(_itemname);
                                item.SetVal(val == "On");
                            }
                        }

                        break;
                }
            }
        }

        private List<JSONStorableActionStringChooser> actions;
        private JSONStorableStringChooser _test;
        private List<JSONStorable> _dockedUIs;
        private List<string> _personUids;
        private SupportedPlugins.SupportedPluginManager _pluginManager;
        private JSONStorableStringChooser _personChooser;
        private Dictionary<string, List<JSONStorable>> _loadedPlugins = new Dictionary<string, List<JSONStorable>>();

        // private struct enabledPlugin
        // {
        //     public string name;
        //     public bool filter;
        //
        //     public enabledPlugin(string name, bool filter, ISupportedPlugin plugin) : this()
        //     {
        //         this.name = name;
        //         this.filter = filter;
        //         this.plugin = plugin;
        //     }
        // }

        public override void Init()
        {
            _pluginManager = new SupportedPlugins.SupportedPluginManager(this);
            _personChooser = new JSONStorableStringChooser(
                "PersonSelector",
                SuperController.singleton.GetAtoms().FindAll(atom => atom.type == "Person").Select(atom => atom.uid)
                    .ToList(),
                null,
                "Person Selector"
            );
            RegisterStringChooser(_personChooser);
            _personChooser.setCallbackFunction += val => findPlugins(true);
            CreatePopup(_personChooser);
            JSONStorableBool reloadFilterPerson = new JSONStorableBool("Filter Person on reload", true);
            RegisterBool(reloadFilterPerson);
            CreateToggle(reloadFilterPerson);
            JSONStorableAction reload =
                new JSONStorableAction("RefreshPlugins", () => { findPlugins(reloadFilterPerson.val); });
            reload.RegisterButton(CreateButton("Refresh Plugins"));
            findPlugins(false);
        }

        public void atomAdd(Atom atom)
        {
            SuperController.LogMessage("atomadd: " + atom.name);
        }

        public void atomChange(List<string> atomUIDs)
        {
            SuperController.LogMessage("atomchange: " + atomUIDs);
        }

        public void DockedUICountChanged(JSONStorable dockedUI, float count)
        {
            SuperController.LogMessage("Button Added to " + dockedUI.storeId);
        }

        private void findPlugins(bool filterOnly)
        {
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                _pluginManager.ProcessAtom(atom, filterOnly);
            }
        }

        public static JSONStorable FindStorable(Atom atom, string storeID)
        {
            var storable = atom.GetStorableByID(storeID);
            if (storable != null)
            {
                return storable;
            }
            else
            {
                return null;
            }
        }
    }
}