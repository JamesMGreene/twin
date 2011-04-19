// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.util.*;

import org.ebayopensource.twin.json.JSONable;

/**
 * A persistent object exposed by the server.
 * <p>
 * This is a low-level representation typically wrapped by an Element etc, and should not be used directly.
 */
class RemoteObject implements JSONable {
	/** The .NET or Java class name of this object */
	public final String type;
	/** The object's UUID on the server */
	public final String uuid;
	/** The application session that this object is part of */
	public final Application session;
	/** The extra properties passed to the client along with the object */
	public final Map<String,Object> properties;
	/** Internal only */
	public RemoteObject(Application session, String type, String uuid, Map<String,Object> properties) {
		this.type = type;
		this.uuid = uuid;
		this.session = session;
		this.properties = properties;
	}
	
	public Object toJSON() {
		// When serialising back to the server we really just need uuid
		Map<String,Object> jsonObject = new HashMap<String,Object>();
		jsonObject.put("class", type);
		jsonObject.put("uuid", uuid);
		return jsonObject;
	}
	public String toString() {
		return type+":"+uuid;
	}
	
	public boolean equals(Object other) {
		if(!(other instanceof RemoteObject))
			return false;
		RemoteObject otherRemote = (RemoteObject)other;
		return session == otherRemote.session && uuid.equals(otherRemote.uuid);
	}
	
	public int hashCode() {
		return session.hashCode() ^ uuid.hashCode();
	}
}
