using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MinnBicchi.SupportedPlugins
{
    public abstract class SupportedPlugin<TPluginActions> where TPluginActions : PluginActions, new()
    {
        protected readonly JSONStorable _storable;
        protected QuickActions _script;
        protected string _parentUid;
        protected Regex _actionParser;
        protected TPluginActions _pluginActions;

        public SupportedPlugin(JSONStorable storable, string parentUid, QuickActions script)
        {
            _storable = storable;
            _script = script;
            _parentUid = parentUid;
            _pluginActions = new TPluginActions();
        }

        public string Name()
        {
            return _storable.name + " QuickActions";
        }

        public abstract IEnumerator<WaitForEndOfFrame> InitActions(JSONStorable actionStorable);
    }
}