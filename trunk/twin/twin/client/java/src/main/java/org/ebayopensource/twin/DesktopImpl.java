// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.util.Collections;

import org.ebayopensource.twin.element.Desktop;
import org.ebayopensource.twin.pattern.ControlPattern;

/** @see ElementImpl */
class DesktopImpl extends ElementImpl implements Desktop {
	public DesktopImpl(Application session) {
		super(session, Desktop.class, Collections.<Class<? extends ControlPattern>>emptyList());
	}

	@Override public String getPath() {
		return "/desktop";
	}
	@Override public String getName() {
		return "Desktop";
	}
	@Override public boolean exists() {
		return true;
	}
	@Override public ScrollBar getScrollBar(ScrollBar.Orientation orientation) {
		throw new IllegalArgumentException("The desktop has no scroll bars");
	}
	@Override public Element getParent() {
		return null;
	}
	@Override public boolean isEnabled() {
		return true;
	}
	
	public int hashCode() {
		return Desktop.class.hashCode() ^ session.hashCode();
	}
	public boolean equals(Object other) {
		if(!(other instanceof Desktop))
			return false;
		if(!session.equals(((Element)other).getApplication()))
			return false;
		return true;
	}
}
