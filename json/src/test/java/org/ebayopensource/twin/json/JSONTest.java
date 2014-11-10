// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.json;

import java.util.*;

import org.testng.annotations.*;
import static org.testng.AssertJUnit.*;

public class JSONTest {
	private enum Colors {
		Red,
		Green,
		Blue
	}
	
	private Map<String,Object> data;
	public JSONTest() {
		data = new HashMap<String,Object>();
		data.put("integer", 3);
		data.put("double", 2.5);
		data.put("string", "foo");
		data.put("array", new Object[]{ 1,2,3, null});
		data.put("list", Arrays.asList(1,2,3, null));
		data.put("null", null);
	}
	
	@Test 
	public void testBasicValueDecode() {
		assertEquals("string", "test", JSON.decode("\"test\""));
		assertEquals("integer", 3, JSON.decode("3"));
		assertEquals("double", 2.5, JSON.decode("2.5"));
		assertEquals("boolean", true, JSON.decode("true"));
		assertEquals("null", null, JSON.decode("null"));
	}
	
	@Test
	public void testStringEscapeEncode() {
		assertEquals("string escape", "\"abc\\\"\\u0000\\r\\n\\b\\f\\t\\\\\"", JSON.encode("abc\"\u0000\r\n\b\f\t\\"));
	}
	@Test
	public void testStringEscapeDecode() {
		assertEquals("string escape", "abc\"\u0000\r\n\b\f\t\\", JSON.decode("\"abc\\\"\\u0000\\r\\n\\b\\f\\t\\\\\""));		
	}
	
	@Test 
	public void testBasicValueEncode() {
		assertEquals("string", "\"test\"", JSON.encode("test"));
		assertEquals("integer", "3", JSON.encode(3));
		assertEquals("double", "2.5", JSON.encode(2.5));
		assertEquals("boolean", "true", JSON.encode(true));
		assertEquals("null", "null", JSON.encode(null));
	}
	
	@Test
	public void testDecodeMap() {
		String json = "{\"integer\":3, \"double\":2.5, \"string\": \"foo\", \"array\":[1,2,3], \"list\":[1,2,3], \"null\":null }";
		Object result = JSON.decode(json);
		assertTrue("result is a map", result instanceof Map<?,?>);
		Map<?,?> map = (Map<?,?>)result;
		assertEquals("integer", 3, map.get("integer"));
		assertEquals("double", 2.5, map.get("double"));
		assertEquals("string", "foo", map.get("string"));
		assertEquals("list", Arrays.asList(1,2,3), map.get("list"));
		assertEquals("array", Arrays.asList(1,2,3), map.get("array"));
		assertTrue("Map contains 'null' key", map.containsKey("null"));
		assertEquals("null", null, map.get("null"));
	}
	
	@Test 
	public void testEncodeEnum() {
		List<?> pre = Arrays.asList(Colors.Red);
		List<?> post = Arrays.asList("Red");
		
		String json = JSON.encode(pre);
		Object result = JSON.decode(json);
		assertEquals(post, result);
	}
	
	@Test
	public void testEncodeMap() {
		String json = JSON.toString(data);
		Object result = JSON.decode(json);
		assertTrue("result is a map", result instanceof Map<?,?>);
		Map<?,?> map = (Map<?,?>)result;
		assertEquals("integer", 3, map.get("integer"));
		assertEquals("double", 2.5, map.get("double"));
		assertEquals("string", "foo", map.get("string"));
		assertEquals("list", Arrays.asList(1,2,3, null), map.get("list"));
		assertEquals("array", Arrays.asList(1,2,3, null), map.get("array"));
		assertTrue("Map contains 'null' key", map.containsKey("null"));
		assertEquals("null", null, map.get("null"));
	}
	
	@Test
	public void testEncodeFailure() throws Exception {
		java.net.URL url = new java.net.URL("http://google.com");
		try {
			String encodedURL = JSON.encode(url);
			assertTrue("Should fail to encode unknown type but URL encodes to: "+encodedURL, false);
		} catch (IllegalArgumentException e) {}
		try {
			String encodedURL = JSON.encode(Arrays.asList(url));
			assertTrue("Should fail to encode unknown type but List containing URL encodes to: "+encodedURL, false);
		} catch (IllegalArgumentException e) {}
	}
}
