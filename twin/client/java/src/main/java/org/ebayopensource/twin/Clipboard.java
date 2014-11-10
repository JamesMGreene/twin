// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.util.*;

/** Represents the remote clipboard */
public class Clipboard {
	private Application session;
	Clipboard(Application session) {
		this.session = session;
	}
	
	private String getPath() {
		return "/clipboard";
	}
	
	/** Gets the current text on the clipboard.
	 * @return the text on the clipboard, if the clipboard is not empty and the data on the clipboard can be converted to text. 
	 * Null if this is not the case.
	 * @throws TwinException
	 */
	public String getText() throws TwinException {
		Map<String,Object> data = session.requestObject("GET", getPath(), null);
		if(!"text".equalsIgnoreCase((String)data.get("type")))
			return null;
		return (String)data.get("text");
	}
	/** Sets the content of the remote clipboard to the specified string */
	public void setText(String text) throws TwinException {
		Map<String,Object> data = new HashMap<String,Object>();
		data.put("type", "text");
		data.put("text", text);
		session.request("POST", getPath(), data);
	}
	/** Clears the clipboard */
	public void clear() throws TwinException {
		session.request("DELETE", getPath(), null);
	}
	/** Determines whether there is no data on the clipboard.
	 * Note that this method can return false even if getText() returns null, e.g. if the clipboard contains an image.
	 * @return true is the clipboard is completely empty.
	 * @throws TwinException
	 */
	public boolean isEmpty() throws TwinException {
		Map<String,Object> data = session.requestObject("GET", getPath(), null);
		return data.get("type") == null;	
	}
}
