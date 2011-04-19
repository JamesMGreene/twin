// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.util.*;
import org.ebayopensource.twin.json.JSONable;
import org.ebayopensource.twin.element.*;
import org.ebayopensource.twin.pattern.*;

/**
 * A set of conditions that an element might match, which can be used for searching the element tree.
 * <p>
 * Criteria are evaluated on the server as part of a search, and determine what elements you will get back.
 * <p>
 * There are two basic type of criteria: <ul>
 * <li>Property criteria, that check whether a property of an element (like controlType) has a certain value (like Window)</li>
 * <li>Compound criteria, that let you combine other criteria with and, or, and not operations</li>
 * </ul>
 * <p>
 * Rather than instantiating criteria directly, use the static methods of Criteria.
 * For easiest use of these, you should <code>static import org.ebayopensource.twin.Critera.*;</code> 
 * This allows you to write criteria like name("foo").and(type(ControlType.Button)).
 * <p>
 * TODO we could evaluate these clientside too - is there any need?
 */
public abstract class Criteria implements JSONable {
	/** Internal class to represent an AND and OR criterion */
	private static class Conjunction extends Criteria {
		public Conjunction(boolean and, Criteria... criteria) {
			this.and = and;
			this.criteria = criteria;
		}
		boolean and;
		Criteria[] criteria;

		public Object toJSON() {
			Map<String,Object> data = new HashMap<String,Object>();
			data.put("type", and ? "and" : "or");
			Object[] jsonCriteria = new Object[criteria.length];
			for(int i=0; i<criteria.length; i++)
				jsonCriteria[i] = criteria[i].toJSON();
			data.put("target", jsonCriteria);
			return data;
		}
		
		public String toString() {
			StringBuffer sb = new StringBuffer("(");
			for(int i=0; i<criteria.length; i++) {
				if(i>0)
					sb.append(and?" and ":" or ");
				sb.append(criteria[i]);
			}
			sb.append(')');
			return sb.toString();
		}
	}
	/** Internal class to represent a property=value criterion */
	private static class PropertyEquals extends Criteria {
		public PropertyEquals(String propertyName, Object propertyValue) {
			this.propertyName = propertyName;
			this.propertyValue = propertyValue;
		}
		String propertyName;
		Object propertyValue;
		public Object toJSON() {
			Map<String,Object> data = new HashMap<String,Object>();
			data.put("type","property");
			data.put("name", propertyName);			
			data.put("value", propertyValue);			
			return data;
		}
		public String toString() {
			return propertyName + "=" + propertyValue;
		}
	}
	/** Internal class to represent a NOT criterion */
	private static class Negate extends Criteria {
		public Negate(Criteria c) {
			this.criteria = c;
		}
		Criteria criteria;
		public Object toJSON() {
			Map<String,Object> data = new HashMap<String,Object>();
			data.put("type","not");
			data.put("target",criteria.toJSON());
			return data;
		}
		public String toString() {
			return "not "+criteria;
		}
	}
	
	/** A compound criterion that matches if ALL components match */
	public static Criteria and(Criteria... list) {
		return new Conjunction(true, list);
	}
	/** A compound criterion that matches if ANY component matches */
	public static Criteria or(Criteria... list) {
		return new Conjunction(false, list);
	}
	/** A compound criterion that matches if its component does NOT match */
	public static Criteria not(Criteria c) { 
		return c.not(); 
	}

	/** A criterion that matches if <code>this</code> does NOT match */
	public Criteria not() {
		return new Negate(this);
	}
	/** A criterion that matches if <code>this</code> AND <code>other</code> both match */
	public Criteria and(Criteria other) {
		return new Conjunction(true, this, other);
	}
	/** A criterion that matches if either <code>this</code> OR <code>other</code> match */
	public Criteria or(Criteria other) {
		return new Conjunction(false, this, other);
	}	
	
	/** A criterion that matches if the element's name is <code>name</code> */
	public static Criteria name(String name) {
		return equals("name", name);
	}
	/** A criterion that matches if the element's controlType is <code>type</code> */
	public static Criteria type(Class<?> type) {
		if(ElementImpl.isInterfaceExtending(type, ControlType.class))
			return equals("controlType", type.getSimpleName());
		if(ElementImpl.isInterfaceExtending(type, ControlPattern.class))
			return equals("controlPattern", type.getSimpleName());
		throw new IllegalArgumentException("type must be a ControlType or ControlPattern but "+type.getName()+" is neither");
	}
	/** A criterion that matches if the element's className is <code>className</code> */
	public static Criteria className(String className) {
		return equals("className", className);
	}
	/** A criterion that matches if the element's id is <code>id</code> */
	public static Criteria id(String id) {
		return equals("id", id);
	}
	/** A criterion that matches if the element's enabled state is <code>enabled</code> */
	public static Criteria enabled(boolean enabled) {
		return equals("enabled", enabled);
	}
	/** A criterion that matches if the element's string value is <code>value</code> */
	public static Criteria value(String value) {
		return equals("value", value);
	}
	/**
	 * A criterion that matches if the element's <code>property</code> property is <code>value</code>
	 * The server must support querying by <code>property</code>.
	 */
	public static Criteria equals(String property, Object value) {
		return new PropertyEquals(property, value);
	}
}
