// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.element;

import org.ebayopensource.twin.*;

/**
 * A MenuItem is a text element that is typically displayed either inside a Menu, or in a window in order to activate a Menu.
 */
public interface MenuItem extends ControlType {
	/** 
	 * Click on this menu item, which is expected to spawn a menu. 
	 * @return the menu opened
	 * @throws TwinException if the menu couldn't be found
	 */
	@IDE public Menu openMenu() throws TwinException;
}
