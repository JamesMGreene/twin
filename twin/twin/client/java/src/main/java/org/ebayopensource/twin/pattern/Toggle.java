// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import org.ebayopensource.twin.*;

/** An item that can be turned on or off */
@Name("toggle")
public interface Toggle extends ControlPattern {
	/** Get the current toggle state. */
	@IDE(attribute=true) public boolean getState() throws TwinException;
	/** Set the toggle state. */
	@IDE public void setState(boolean b) throws TwinException;
	/** Toggle the item. 
	 * @return the new state
	 */
	public boolean toggle() throws TwinException;
}
