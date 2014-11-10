// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

/** 
 * This subclass of TwinException is thrown when we try to use an element that isn't present anymore.
 * 
 * TODO somehow bundle the element reference along with this exception, the IDE needs it.
 */
@SuppressWarnings("serial")
public class TwinStaleElementException extends TwinException {
	public TwinStaleElementException(String s) {
		super(s);
	}
}
