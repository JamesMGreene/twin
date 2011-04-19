// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.json;

/** 
 * Used by objects to implement custom serialization.
 * <p>
 * If an object implements JSONable, when it is serialized its toJSON() method will be called 
 * and the result will be serialized in its place.
 */
public interface JSONable {
	/** @return an intermediate form in the serialization of this object */
	public Object toJSON();
}
