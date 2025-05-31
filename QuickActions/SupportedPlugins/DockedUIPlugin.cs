using System;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace MinnBicchi.SupportedPlugins
{
    public class DockedUIPluginActions : PluginActions
    {
        private static Regex _actionParser = new Regex("^([\\w ]+) (Show|Hide)$", RegexOptions.Compiled);
        
        public void LoadActions(JSONStorable pluginStorable, JSONStorable actionStorable)
        {
            foreach (var actionName in pluginStorable.GetActionNames())
            {
                addActionFilter(pluginStorable, actionName, actionStorable);
            }
        }
        private void addActionFilter(JSONStorable pluginStorable, string actionName, JSONStorable actionStorable)
        {
            var m = _actionParser.Match(actionName);
            if (m.Success)
            {
                var actionBaseName = m.Groups[1].Value;
                var actionType = m.Groups[2].Value;
                AddActionToGroup(actionBaseName,
                    actionType,
                    new StorableAction(pluginStorable.containingAtom.uid, pluginStorable.storeId, actionName));
                var actionStoreName = actionBaseName + actionType;
                var jsa = actionStorable.GetAction(actionStoreName);
                if (jsa == null)
                {
                    jsa = new JSONStorableAction(actionStoreName, () => ActionCallback(actionBaseName));
                
                }
                actionStorable.RegisterAction(jsa);
                AddActionToGroup(actionBaseName, "_onPress", new StorableAction(jsa));
            }
        }
        
        public void ActionCallback(string name)
        {
            SuperController.LogMessage(name + " Clicked");
        }
    }
    public class DockedUIPlugin : SupportedPlugin<DockedUIPluginActions>
    {
        private static Regex _widgetParser = new Regex("^([\\w ]+)Widget(\\d+)$", RegexOptions.Compiled);
        private JSONStorableAction _refresh;
        private JSONStorable _pluginStorable;
        
        public DockedUIPlugin(JSONStorable storable, string parentUid, QuickActions script) : base(storable,
            parentUid,
            script)
        {
            _pluginStorable = storable;
            _refresh = new JSONStorableAction("Refresh DockedUI", RefreshDockedUI);
            _refresh.RegisterButton(script.CreateButton("Refresh DockedUI"));
        }


        public void RefreshDockedUI()
        {
            foreach (var name in _storable.GetActionNames())
            {
                SuperController.LogMessage(name);
                _storable.GetAction(name);
            }
        }

        private struct dockedUIWidget
        {
            public string ItemKey;
            public string ItemName;
            public string ItemType;

            public dockedUIWidget(string itemKey, string itemName, string itemType)
            {
                this.ItemKey = itemKey;
                this.ItemName = itemName;
                this.ItemType = itemType;
            }
        }

        private List<dockedUIWidget> getWidgetsFromJson(JSONClass jc)
        {
            var widgets = new List<dockedUIWidget>();
            foreach (var jsonKey in jc.Keys)
            {
                var m = _widgetParser.Match(jsonKey);
                if (m.Success)
                {
                    if (!jc[jsonKey].AsObject.HasKey("name"))
                    {
                        continue;
                    }
                    SuperController.LogMessage(jc[jsonKey]["name"].Value + m.Groups[2].Value + " : " +
                                               jc[jsonKey]["name"].Value + " : " + m.Groups[1].Value);
                    widgets.Add(new dockedUIWidget(jc[jsonKey]["name"].Value + m.Groups[2].Value,
                        jc[jsonKey]["name"].Value, m.Groups[1].Value));
                }
            }

            return widgets;
        }
        /// <summary>
        /// Scans the DockedUI plugin
        /// Collects DockedUI actions and storables that we support and stores them for later(Show/Hide/Position etc)
        /// Generates or restores the new Actions to use as a trigger for the Widget.
        /// Adds generated Actions to a storable on the same atom as the DockedUI plugin. 
        /// Sets the DockedUI triggers using JSON injection.
        /// </summary>
        /// <param name="actionStorable">The Storable to use to keep my generated storables</param>
        /// <returns></returns>
        public override IEnumerator<WaitForEndOfFrame> InitActions(JSONStorable actionStorable)
        {
            var validActions = new DockedUIPluginActions();
            validActions.LoadActions(_pluginStorable, actionStorable);
            yield return new WaitForEndOfFrame();
            var jc = _pluginStorable.GetJSON();
            var widgets = getWidgetsFromJson(jc);
            foreach (var widget in widgets)
            {
                buildTriggers(actionStorable, validActions, widget, jc);
            }
            validActions.GetMissingActions(_pluginActions, actionStorable)
                .ForEach(action => actionStorable.DeregisterAction(action));
            _pluginActions = validActions;
            yield return new WaitForEndOfFrame();
            _pluginStorable.RestoreFromJSON(jc); // Update DockedUI to include our generated Triggers
        }
        
        
        /// <summary>
        /// Builds a SimpleJSON.JSONObject to update the DockedUI widget Triggers (the action assigned to a Button etc)
        /// DockedUI's JSON object is modified in place  
        /// </summary>
        /// <param name="parentStorable">Storable containing the Generated Actions to assign to the Widget trigger</param>
        /// <param name="validActions">List of actions, to make sure we only load actions we actually generated</param>
        /// <param name="widget">The important values from the JSONObject representing the DockedUI's Widget</param>
        /// <param name="jc">The JSON class representing the full DockedUI plugin in JSON format</param>
        private static void buildTriggers(JSONStorable parentStorable, DockedUIPluginActions validActions, dockedUIWidget widget,
            JSONClass jc)
        {
            if (validActions.ContainsKey(widget.ItemName))
            {
                var trigger = new JSONClass();
                var triggerName = widget.ItemName + "_QuickActionHook";
                trigger.Add("name", triggerName);
                trigger.Add("receiverAtom", parentStorable.containingAtom.uid);
                trigger.Add("receiver", parentStorable.name);
                trigger.Add("receiverTargetName", 
                    validActions.GetActionGroup(widget.ItemName).GetAction("_onPress").name);
                var triggersNode = jc[widget.ItemKey].AsObject;
                findReplaceOrAddTrigger(triggersNode, triggerName, trigger);
                if (jc.HasKey("Actions"))
                {
                    var actionsNode = jc["Actions"].AsObject;
                    if (actionsNode.HasKey(widget.ItemKey))
                    {
                        var actionsItemTriggers = actionsNode[widget.ItemKey].AsObject;
                        findReplaceOrAddTrigger(actionsItemTriggers, triggerName, trigger);
                    }
                }
            }
        }

        private static void findReplaceOrAddTrigger(JSONClass triggersNode, string triggerName, JSONClass trigger)
        {
            if (!triggersNode.HasKey("startActions"))
            {
                triggersNode.Add("startActions", new JSONArray());
                triggersNode["startActions"].Add(trigger);
                return;
            }

            JSONArray startActions = triggersNode["startActions"].AsArray;
            for (int i = 0; i < startActions.Count; i++)
            {
                var t = startActions[i].AsObject;
                {
                    SuperController.LogMessage(i.ToString());
                    if (t.HasKey("name"))
                    {
                        SuperController.LogMessage(t["name"]);
                        if (triggerName == t["name"].Value)
                        {
                            startActions[i] = trigger;
                            return;
                        }
                    }
                }
            }

            triggersNode["startActions"].Add(trigger);
        }
    }
}