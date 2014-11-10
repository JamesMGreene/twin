// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import org.ebayopensource.twin.*;

@Name("expand")
public interface Expandable extends ControlPattern {
	/** Find out if the element is expanded or collapsed. 
	 * <p>
	 * Required patterns: ExpandCollapsePattern. Returns true if state is Expanded or PartiallyExpanded.
	 */
	@IDE(attribute=true) public boolean isExpanded() throws TwinException;
	/** Expand or collapse the item. 
	 * <p>
	 * Required patterns: ExpandCollapsePattern. Calls Expand() or Collapse().
	 */
	@IDE public void setExpanded(boolean expanded) throws TwinException;
}
