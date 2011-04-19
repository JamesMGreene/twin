// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import org.ebayopensource.twin.Element;

/** 
 * Marker interface for interfaces that represent control patterns.
 * <p>
 * This is used to detect cases where e.g.:
 * 
 *    TreeItem extends Expandable
 * 	  Proxy$1 is a proxied ElementImpl that represents a leafnode TreeItem.
 *    Proxy$1 doesn't implement Expandable directly
 *    We try to call Proxy$1.setExpanded()
 * 
 * Now we can notice that setExpanded isn't exposed by any control pattern - Proxy$1 doesn't directly implement any 
 * interface that directly extends ControlPatten and defines setExpanded(). And thus veto the method call.
 * <p>
 * Note that if this method doesn't seem robust maybe we'll use annotations or something instead to get this data
 * but a base marker interface is still nice documentation :)
 */
public interface ControlPattern extends Element {
}
