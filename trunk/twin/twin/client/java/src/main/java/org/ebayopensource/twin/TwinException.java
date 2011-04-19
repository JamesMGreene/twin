// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

/** 
 * An exception thrown by Twin.
 * <p>
 * Notable subclasses are {@link org.ebayopensource.twin.TwinRemoteException TwinRemoteException} for exceptions thrown on the server and 
 * {@link org.ebayopensource.twin.TwinTimeoutException TwinTimeoutException} for exceptions caused by time outs}.
 */
public class TwinException extends RuntimeException {
	private static final long serialVersionUID = 57903202676793096L;
	
	public TwinException(String message) {
		super(message);
	}
	
	// We allow overriding the class name for remote exceptions.
	/** The remote class name */
	String className;
	public String toString() {
		if(className == null)
			return super.toString();
		if(getMessage() == null)
			return className;
		return className+": "+getMessage();
	}
}
