// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.pattern;

import org.ebayopensource.twin.*;

@Name("edit")
public interface Editable extends ControlPattern {
	/** Get the text value of the element (e.g. of an edit box) */
	@IDE(attribute=true) public String getValue() throws TwinException;
	/** Set the text value of the element (e.g. of an edit box) */
	@IDE public void setValue(String s) throws TwinException;
	/** Determine whether the value can be set. */
	@IDE(attribute=true) public boolean isReadOnly() throws TwinException;
}
