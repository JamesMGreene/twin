// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.element;

/** 
 * The Desktop is the root of the element tree.
 * <p>
 * It has name "Desktop" and controlType Pane, and its children are the top-level windows of the application.
 * <p>
 * Taking screenshots of the desktop takes images from the actual screen rather than of any particular element.
 */
public interface Desktop extends ControlType {
}
