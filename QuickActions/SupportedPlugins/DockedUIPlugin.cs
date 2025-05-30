using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SimpleJSON;

namespace MinnBicchi.SupportedPlugins
{
    public class DockedUIPlugin : SupportedPlugin
    {
        private static Regex _actionParser = new Regex("^([\\w ]+) (Show|Hide)$", RegexOptions.Compiled);
        private static Regex _widgetParser = new Regex("^([\\w ]+)Widget(\\d+)$", RegexOptions.Compiled);
        private JSONStorableAction _refresh;
        private JSONStorable _pluginStorable;

        public DockedUIPlugin(JSONStorable storable, string parentUid, QuickActions script) : base(storable, parentUid,
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

        public override IEnumerator InitActions(JSONStorable parentStorable)
        {
            var _actions = new Dictionary<string, Dictionary<string, JSONStorableAction>>();
            foreach (var actionName in _storable.GetActionNames())
            {
                var m = _actionParser.Match(actionName);
                if (m.Success)
                {
                    if (!_actions.ContainsKey(m.Groups[1].Value))
                    {
                        _actions[m.Groups[1].Value] = new Dictionary<string, JSONStorableAction>();
                    }

                    _actions[m.Groups[1].Value][m.Groups[2].Value] = _storable.GetAction(actionName);
                }

                yield return new WaitForEndOfFrame();
            }

            var validActions = new Dictionary<string, Dictionary<string, JSONStorableAction>>();
            var json = _pluginStorable.GetJSON();
            // SuperController.LogMessage(json.ToString());
            foreach (var action in _actions)
            {
                // SuperController.LogMessage(action.Key);
                if (action.Value.ContainsKey("Show") && action.Value.ContainsKey("Hide"))
                {
                    validActions[action.Key] = new Dictionary<string, JSONStorableAction>();
                    validActions[action.Key]["Show"] = action.Value["Show"];
                    validActions[action.Key]["Hide"] = action.Value["Hide"];
                    var actionStoreName = action.Key + "_onPress";
                    var jsa = parentStorable.GetAction(actionStoreName);
                    if (jsa == null)
                    {
                        jsa = new JSONStorableAction(actionStoreName, () => ActionCallback(action.Key));
                        parentStorable.RegisterAction(jsa);
                    }

                    validActions[action.Key]["onPress"] = jsa;
                }

                yield return new WaitForEndOfFrame();
            }
            foreach (var jsonKey in json.Keys)
            {
                var m = _widgetParser.Match(jsonKey);
                if (m.Success)
                {
                    if (!json[jsonKey].AsObject.HasKey("name"))
                    {
                        continue;
                    }

                    var itemName = json[jsonKey]["name"].Value;
                    var itemKey = itemName + m.Groups[2].Value;
                    SuperController.LogMessage("Key: " + itemKey);
                    if (json.HasKey(itemKey))
                    {
                        var trigger = new JSONClass();
                        trigger.Add("name", itemKey);
                        trigger.Add("receiverAtom", parentStorable.containingAtom.uid);
                        trigger.Add("receiver", parentStorable.name);
                        trigger.Add("receiverTargetName", validActions[itemName]["onPress"].name);
                        if (!json[itemKey].AsObject.HasKey("startActions"))
                        {
                            json[itemKey].Add("startActions", new JSONArray());
                            json[itemKey]["startActions"].Add(trigger);
                        }
                        else
                        {
                            json[itemKey]["startActions"].Add(trigger);
                        }
                        if (json.HasKey("Actions"))
                        {
                            if (json["Actions"].AsObject.HasKey(itemKey))
                            {
                                if(!json["Actions"][itemKey].AsObject.HasKey("startActions"))
                                {
                                    json["Actions"][itemKey].Add("startActions", new JSONArray());
                                }
                                json["Actions"][itemKey]["startActions"].AsArray.Add(trigger);
                            }
                        }
                    }
                    else
                    {
                        SuperController.LogMessage("Found widget without object  " + itemKey + "  : " +
                                                   m.Groups[0].Value);
                    }
                }
            }
            _pluginStorable.RestoreFromJSON(json);
            SuperController.LogMessage(json.ToString());
        }

        public void ActionCallback(string name)
        {
            SuperController.LogMessage(name + " Clicked");
        }
    }
}