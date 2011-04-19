// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.util.*;

import org.ebayopensource.twin.element.*;
import org.ebayopensource.twin.pattern.*;

@SuppressWarnings("unchecked")
public class NameMappings {
	private static final Class<?>[] CONTROL_PATTERNS = {
		Editable.class,
		Expandable.class,
		Selectable.class,
		SelectionContainer.class,
		Toggle.class,
		Transformable.class,
	};

	private static final Class<?>[] CONTROL_TYPES = {
		Button.class,
		CalendarControl.class,
		CheckBox.class,
		ComboBox.class,
		Custom.class,
		DataGrid.class,
		DataItem.class,
		Desktop.class,
		Document.class,
		Edit.class,
		Group.class,
		Header.class,
		HeaderItem.class,
		Hyperlink.class,
		Image.class,
		ListControl.class,
		ListItem.class,
		Menu.class,
		MenuBar.class,
		MenuItem.class,
		Pane.class,
		ProgressBar.class,
		RadioButton.class,
		ScrollBarControl.class,
		Separator.class,
		Slider.class,
		Spinner.class,
		SplitButton.class,
		StatusBar.class,
		Tab.class,
		TabItem.class,
		Table.class,
		Text.class,
		Thumb.class,
		TitleBar.class,
		ToolBar.class,
		ToolTip.class,
		Tree.class,
		TreeItem.class,
		Window.class,
	};
	
	private static Map<String,Class<? extends ControlPattern>> patternNameToInterface = new HashMap<String,Class<? extends ControlPattern>>();
	private static Map<Class<? extends ControlPattern>,String> patternInterfaceToName = new HashMap<Class<? extends ControlPattern>,String>();
	private static Map<String,Class<? extends ControlType>> typeNameToInterface = new HashMap<String,Class<? extends ControlType>>();
	private static Map<Class<? extends ControlType>,String> typeInterfaceToName = new HashMap<Class<? extends ControlType>,String>();
	static {
		for(Class<?> pattern : CONTROL_PATTERNS) {
			Name nameAnnotation = pattern.getAnnotation(Name.class);
			String name = nameAnnotation == null ? pattern.getSimpleName() : nameAnnotation.value();
			if(!ControlPattern.class.isAssignableFrom(pattern))
				throw new IllegalStateException(pattern+" is not a ControlPattern");
			patternNameToInterface.put(name, (Class<? extends ControlPattern>)pattern);
			patternInterfaceToName.put((Class<? extends ControlPattern>)pattern, name);
		}
		for(Class<?> type : CONTROL_TYPES) {
			Name nameAnnotation = type.getAnnotation(Name.class);
			String name = nameAnnotation == null ? type.getSimpleName() : nameAnnotation.value();
			if(!ControlType.class.isAssignableFrom(type))
				throw new IllegalStateException(type+" is not a ControlPattern");
			typeNameToInterface.put(name, (Class<? extends ControlType>)type);
			typeInterfaceToName.put((Class<? extends ControlType>)type, name);
		}
	}
	public static Class<? extends ControlPattern> getPatternInterface(String name) {
		return patternNameToInterface.get(name);
	}
	public static String getPatternName(Class<? extends ControlPattern> iface) {
		return patternInterfaceToName.get(iface);
	}
	
	public static Class<? extends ControlType> getTypeInterface(String name) {
		return typeNameToInterface.get(name);
	}
	public static String getTypeName(Class<? extends ControlType> iface) {
		return typeInterfaceToName.get(iface);
	}
}
