// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

/** 
 * A remote resource exposed by the server - the base class for Elements, ScrollBars, etc.
 * <p>
 * This typically wraps a RemoteObject, but doesn't have to - e.g. Desktop doesn't have an associated 
 * RemoteObject because it has a fixed URL that's always available.
 */
public abstract class RemoteResource implements RemoteResourceInterface {
	/** The remote object (if any) wrapped */
	protected RemoteObject remote;
	/** The application session this resource is part of */
	protected Application session;
	/** Internal use only: Create a remote resource wrapping a remote object */
	protected RemoteResource(RemoteObject remote) {
		this.remote = remote;
		this.session = remote.session;
	}
	/** Internal use only: create a remote resource that does not wrap a remote object */
	protected RemoteResource(Application session) {
		this.remote = null;
		this.session = session;
	}
	public RemoteObject getRemote() {
		return remote;
	}
	/** Get the application that this resource is part of */
	public Application getApplication() {
		return session;
	}
	/** Get the path of this resource within the application, such as /element/12345 */
	protected abstract String getPath();
	public String toString() {
		return this.getClass().getSimpleName() + "("+remote+")";
	}
	
	/** 
	 * If the resource has an associated object, equal if the objects are equal (i.e. the server has the same GUID for them).
	 * <p>
	 * If there is no associated object, objects are equal if they compare reference-equal.
	 * <p>
	 * The server should implement this so that GUIDs are equal where objects compare equal. This is not required to hold if 
	 * one of the objects is expired, so clients should not expire items until they are no longer reachable (i.e. finalized in java).
	 * <p>
	 * Note that the current client doesn't expire any items, they just go out of scope when the session is closed.
	 */
	public boolean equals(Object other) {
		if(remote == null)
			return this == other;
		if(!(other instanceof RemoteResourceInterface)) // instanceof RemoteResource fails for proxied elements
			return false;
		return remote.equals(((RemoteResourceInterface)other).getRemote());
	}
	
	/**
	 * @see #equals(Object)
	 */
	public int hashCode() {
		if(remote == null)
			return super.hashCode();
		return remote.hashCode();
	}
}
