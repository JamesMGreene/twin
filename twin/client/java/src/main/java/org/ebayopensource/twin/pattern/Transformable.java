// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import org.ebayopensource.twin.*;

@Name("transform")
public interface Transformable extends ControlPattern {
	/** Set the size of the element */
	@IDE public void setSize(int width, int height) throws TwinException;
	/** Set the location (top-left corner) of the element on the screen */
	@IDE public void setLocation(int x, int y) throws TwinException;
	/** Set the bounds of the element on the screen */
	public void setBounds(int x, int y, int width, int height) throws TwinException;
}
