// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.json;

import java.util.Map;

/** 
 * Used by JSON for deserialization, the counterpart to JSONable.
 * <p>
 * When a javascript object is encountered during JSON deserialization, it is converted to a map.
 * The recognize() method is called on each recognizer until one returns a non-null value.
 * That value is taken to be the deserialized object. If all recognizers return null, then the map 
 * is the deserialized object.
 * <p>
 * Note that when recognize() is called, all members of the map have all been fully decoded and recognized.
 */
public interface JSONRecognizer {
	/** @return the deserialization of the passed JSON object */
	public Object recognize(Map<String,Object> jsonObject);
}
