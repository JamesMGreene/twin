// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import org.ebayopensource.twin.*;

/** An item that can be selected from a set */
@Name("select")
public interface Selectable extends ControlPattern {
	/** Find out if the element is selected. 
	 * <p>
	 * Required patterns: SelectionItemPattern.
	 */
	@IDE(attribute=true) public boolean isSelected() throws TwinException;
	/** Select or deselect the item. In a multi-select context, adds the item to the selection.
	 * In a single-select context, replaces the selection.
	 * <p>
	 * Required patterns: SelectionItemPattern. Calls Select() or AddToSelection().
	 */
	@IDE public void setSelected(boolean selected) throws TwinException;
	/** Get the SelectionContainer containing this item. */
	@IDE public SelectionContainer getContainer() throws TwinException;
}
