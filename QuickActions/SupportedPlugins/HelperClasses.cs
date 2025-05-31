using System.Collections.Generic;

namespace MinnBicchi.SupportedPlugins
{
    public class RegisteredActions : Dictionary<string, List<JSONStorableAction>>
    {
        public List<JSONStorableAction> GetActionsByBaseName(string baseName)
        {
            List<JSONStorableAction> o;
            this.TryGetValue(baseName, out o);
            return o;
        }

        public void AddRegisteredAction(string baseName, JSONStorableAction jsa)
        {
            List<JSONStorableAction> o;
            this.TryGetValue(baseName, out o);
            if (o == null)
            {
                o = new List<JSONStorableAction>();
                o.Add(jsa);
            }

            this[baseName] = o;
        }
    }

    public abstract class Storable<T>
    {
        protected string _atomUID;
        protected string _storeableID;
        protected string _storeName;

        public Storable(string atomUid, string storeableID, string storeName)
        {
            _atomUID = atomUid;
            _storeableID = storeableID;
            _storeName = storeName;
        }

        public abstract string GetType();

        #region Getters

        public string GetFQN()
        {
            return _atomUID + "/" + _storeableID + "/" + _storeName;
        }

        public string GetName()
        {
            return _storeName;
        }

        public string GetAtomID()
        {
            return _atomUID;
        }

        public string GetStorableID()
        {
            return _storeableID;
        }

        #endregion

        public abstract T GetStorable();
    }

    public class StorableAction : Storable<JSONStorableAction>
    {
        public StorableAction(string atomUid, string storeableID, string storeName) : base(atomUid, storeableID,
            storeName)
        {
        }

        public StorableAction(JSONStorableAction jsa) : 
            base(jsa.storable.containingAtom.uid, jsa.storable.name, jsa.name)
        {
        }

        public override string GetType()
        {
            return "Action";
        }

        public override JSONStorableAction GetStorable()
        {
            return SuperController.singleton.GetAtomByUid(_atomUID).GetStorableByID(_storeableID)
                .GetAction(_storeName);
        }
    }

    public class StorableParam : Storable<JSONStorableParam>
    {
        private string _storeType;

        public StorableParam(string atomUid, string storeableID, string storeName, string storeType) : base(atomUid,
            storeableID,
            storeName)
        {
            _storeType = storeType;
        }

        public override string GetType()
        {
            return _storeType;
        }

        public override JSONStorableParam GetStorable()
        {
            return SuperController.singleton.GetAtomByUid(_atomUID).GetStorableByID(_storeableID)
                .GetParam(_storeName);
        }
    }

    public class PluginActionGroup : Dictionary<string, StorableAction>
    {
        public JSONStorableAction GetAction(string actionType)
        {
            if (this.ContainsKey(actionType))
            {
                return this[actionType].GetStorable();
            }

            return null;
        }

        public void SetAction(string actionType, StorableAction action)
        {
            if (this.ContainsKey(actionType))
            {
                this[actionType] = action;
                return;
            }

            this.Add(actionType, action);
        }

        public List<string> GetActionTypes()
        {
            return this.Keys.ToList();
        }
    }

    public delegate bool ValidStorable();

    public class PluginActions : Dictionary<string, PluginActionGroup>
    {
        protected virtual bool validStoreId(string storeID)
        {
            return false;
        }

        private RegisteredActions _registeredActions = new RegisteredActions();

        public List<string> GetBaseNames()
        {
            return this.Keys.ToList();
        }

        public PluginActionGroup GetActionGroup(string baseName)
        {
            if (this.ContainsKey(baseName))
            {
                return this[baseName];
            }

            return null;
        }

        public void AddActionToGroup(string baseName, string actionType, StorableAction action,
            bool registered = false)
        {
            if (!ContainsKey(baseName))
            {
                this[baseName] = new PluginActionGroup();
            }

            this[baseName].SetAction(actionType, action);
            if (registered)
            {
                _registeredActions.AddRegisteredAction(baseName, action.GetStorable());
            }
        }

        public List<JSONStorableAction> GetMissingActions(PluginActions old, JSONStorable parentStorable)
        {
            List<JSONStorableAction> r = new List<JSONStorableAction>();
            //Find registered actions that are in old but not this (the new one) 
            foreach (var keyValuePair in old)
            {
                if (!ContainsKey(keyValuePair.Key))
                {
                    r.AddRange(_registeredActions.GetActionsByBaseName(keyValuePair.Key));
                }
            }

            //Find registered actions on storable that aren't in this(new)
            //after reload or restore from save TODO change to Regex 
            parentStorable.GetActionNames().ForEach(s =>
            {
                if (s.EndsWith("_onPress"))
                {
                    if (!ContainsKey(s.Remove(s.Length - 8)))
                    {
                        SuperController.LogMessage("Removed: " + s);
                        r.Add(parentStorable.GetAction(s));
                    }
                }
            });
            return r;
        }
    }
}