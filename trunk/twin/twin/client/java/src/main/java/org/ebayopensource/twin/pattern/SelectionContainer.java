// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import java.util.*;

import org.ebayopensource.twin.*;

@Name("select-container")
public interface SelectionContainer extends ControlPattern {
	/** Determine whether multiple items can be selected. */
	@IDE(attribute=true) boolean isMultipleSelectionAllowed() throws TwinException;
	/** Determine whether an item must always be selected. */
	@IDE(attribute=true)boolean isSelectionRequired() throws TwinException;
	/** Get all selected items. */
	@IDE List<Selectable> getSelection() throws TwinException;
	/** Get the single item selected, or null if no item is selected. */
	@IDE Selectable getSelectedItem() throws TwinException;
}
