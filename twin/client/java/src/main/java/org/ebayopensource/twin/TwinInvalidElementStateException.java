// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

/** 
 * This subclass of TwinException is thrown when we attempt to set an element to an invalid state.
 */
@SuppressWarnings("serial")
public class TwinInvalidElementStateException extends TwinException {
	public TwinInvalidElementStateException(String s) {
		super(s);
	}
}
