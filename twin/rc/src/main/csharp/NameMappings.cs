// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;

namespace Twin {
    class NameMappings {
        private static Dictionary<string, AutomationPattern> patterns = new Dictionary<string, AutomationPattern>();
        private static Dictionary<AutomationPattern, string> patternNames = new Dictionary<AutomationPattern, string>();
        private static Dictionary<AutomationPattern, AutomationProperty> patternPresenceProperties = new Dictionary<AutomationPattern, AutomationProperty>();

        private static Dictionary<string, ControlType> types = new Dictionary<string,ControlType>();
        private static Dictionary<ControlType, string> typeNames = new Dictionary<ControlType,string>();

        static NameMappings() {
            patterns["expand"] = ExpandCollapsePattern.Pattern;
            patterns["select"] = SelectionItemPattern.Pattern;
            patterns["select-container"] = SelectionPattern.Pattern;
            patterns["edit"] = ValuePattern.Pattern;
            patterns["toggle"] = TogglePattern.Pattern;
            patterns["transform"] = TransformPattern.Pattern;
            patternPresenceProperties[ExpandCollapsePattern.Pattern] = AutomationElement.IsExpandCollapsePatternAvailableProperty;
            patternPresenceProperties[SelectionItemPattern.Pattern] = AutomationElement.IsSelectionItemPatternAvailableProperty;
            patternPresenceProperties[SelectionPattern.Pattern] = AutomationElement.IsSelectionPatternAvailableProperty;
            patternPresenceProperties[ValuePattern.Pattern] = AutomationElement.IsValuePatternAvailableProperty;
            patternPresenceProperties[TogglePattern.Pattern] = AutomationElement.IsTogglePatternAvailableProperty;
            patternPresenceProperties[TransformPattern.Pattern] = AutomationElement.IsTransformPatternAvailableProperty;

            foreach (KeyValuePair<string, AutomationPattern> kv in patterns)
                patternNames[kv.Value] = kv.Key;

            types["Button"] = ControlType.Button;
            types["CalendarControl"] = ControlType.Calendar;
            types["CheckBox"] = ControlType.CheckBox;
            types["ComboBox"] = ControlType.ComboBox;
            types["Custom"] = ControlType.Custom;
            types["DataGrid"] = ControlType.DataGrid;
            types["DataItem"] = ControlType.DataItem;
            types["Document"] = ControlType.Document;
            types["Edit"] = ControlType.Edit;
            types["Group"] = ControlType.Group;
            types["Header"] = ControlType.Header;
            types["HeaderItem"] = ControlType.HeaderItem;
            types["Hyperlink"] = ControlType.Hyperlink;
            types["Image"] = ControlType.Image;
            types["ListControl"] = ControlType.List; // avoid List datatype 
            types["ListItem"] = ControlType.ListItem;
            types["Menu"] = ControlType.Menu;
            types["MenuItem"] = ControlType.MenuItem;
            types["MenuBar"] = ControlType.MenuBar;
            types["Pane"] = ControlType.Pane;
            types["ProgressBar"] = ControlType.ProgressBar;
            types["RadioButton"] = ControlType.RadioButton;
            types["ScrollBarControl"] = ControlType.ScrollBar; // avoid ScrollBar interface
            types["Separator"] = ControlType.Separator;
            types["Slider"] = ControlType.Slider;
            types["Spinner"] = ControlType.Spinner;
            types["SplitButton"] = ControlType.SplitButton;
            types["StatusBar"] = ControlType.StatusBar;
            types["Tab"] = ControlType.Tab;
            types["TabItem"] = ControlType.TabItem;
            types["Table"] = ControlType.Table;
            types["Text"] = ControlType.Text;
            types["Thumb"] = ControlType.Thumb;
            types["TitleBar"] = ControlType.TitleBar;
            types["ToolBar"] = ControlType.ToolBar;
            types["ToolTip"] = ControlType.ToolTip;
            types["Tree"] = ControlType.Tree;
            types["TreeItem"] = ControlType.TreeItem;
            types["Window"] = ControlType.Window;

            foreach (KeyValuePair<string, ControlType> kv in types)
                typeNames[kv.Value] = kv.Key;
        }

        public static AutomationPattern GetPattern(String name) {
            if (!patterns.ContainsKey(name))
                throw new Exception("Couldn't find AutomationPattern named " + name);
            return patterns[name];
        }
        public static AutomationProperty GetPatternPresenceProperty(AutomationPattern pattern) {
            if (!patternPresenceProperties.ContainsKey(pattern))
                throw new Exception("Couldn't find AutomationPattern presence property for " + pattern.ProgrammaticName);
            return patternPresenceProperties[pattern];
        }
        public static string GetName(AutomationPattern pattern) {
            if (!patternNames.ContainsKey(pattern))
                return null; // can return null if this is not a mapped pattern
            return patternNames[pattern];
        }

        public static ControlType GetType(String name) {
            if (!types.ContainsKey(name))
                throw new Exception("Couldn't find ControlType named " + name);
            return types[name];
        }
        public static string GetName(ControlType type) {
            if (!typeNames.ContainsKey(type))
                throw new Exception("Couldn't find ControlType name for " + type.ProgrammaticName);
            return typeNames[type];
        }
    }
}
