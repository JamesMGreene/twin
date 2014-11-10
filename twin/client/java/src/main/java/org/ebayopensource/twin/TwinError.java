// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.lang.reflect.Constructor;

/**
 * A status code returned by the Twin server. Also used in exceptions generated on the client.
 */
enum TwinError {
	Success(0, null),
	NoSuchElement(1, TwinNoSuchElementException.class),
	NoSuchFrame(2),
	UnknownCommand(9),
	StaleElementReference(10, TwinStaleElementException.class),
	ElementNotVisible(11),
	InvalidElementState(12, TwinInvalidElementStateException.class),
	UnknownError(13),
	ElementNotSelectable(14),
	XPathLookupError(19),
	NoSuchWindow(13),
	InvalidCookieDomain(24),
	CannotSetCookie(25),
	
	// Twin specific
	NoSuchSession(100),
	;
	private TwinError(int code) {
		this(code, TwinException.class);
	}
	private TwinError(int code, Class<? extends TwinException> exceptionType) {
		this.code = code;
		this.exceptionType = exceptionType;
	}
	
	/** The numeric code sent on the wire */
	private int code;
	/** The type of exception to be instantiated */
	private Class<? extends TwinException> exceptionType;

	/** Get a status code from its numeric code */
	public static TwinError get(int code) {
		for(TwinError responseCode : values())
			if(responseCode.code == code)
				return responseCode;
		return TwinError.UnknownError;
	}
	
	public TwinException create(String message) {
		return create(message, null);
	}
	public TwinException create(String message, Throwable cause) {
		if(exceptionType == null)
			throw new IllegalStateException("Tried to create an exception but exceptionType==null for "+this);
		try {
			Constructor<? extends TwinException> c = exceptionType.getConstructor(String.class);
			TwinException ex = c.newInstance(message);
			if(cause != null)
				ex.initCause(cause);
			return ex;
		} catch (Exception e) {
			throw new IllegalStateException("Failed to create exception of type "+this, e);
		}
	}
}
