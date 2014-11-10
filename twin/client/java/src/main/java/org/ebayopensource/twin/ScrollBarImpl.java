// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.util.*;

class ScrollBarImpl extends RemoteResource implements ScrollBar {
	private Orientation orientation;
	private ElementImpl container;
	
	public Orientation getOrientation() {
		return orientation;
	}
	/** Internal, do not use */
	public ScrollBarImpl(RemoteObject object, ElementImpl container, Orientation orientation) {
		super(object);
		this.orientation = orientation;
		this.container = container;
	}
	protected String getPath() {
		return container.getPath() + "/axis/" + remote.uuid;
	}

	public double getPosition() throws TwinException {
		return ((Number)remote.session.request("GET", getPath(), null)).doubleValue();
	}

	public void setPosition(double pos) throws TwinException {
		Map<String,Object> data = new HashMap<String,Object>();
		data.put("position", pos);
		remote.session.request("POST", getPath(), data);
	}
}
