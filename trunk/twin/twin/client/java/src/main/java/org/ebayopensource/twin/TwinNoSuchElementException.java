// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

/** 
 * This subclass of TwinException is thrown when a method expects an element to be present but it isn't.
 */
@SuppressWarnings("serial")
public class TwinNoSuchElementException extends TwinException {
	public TwinNoSuchElementException(String s) {
		super(s);
	}
}
