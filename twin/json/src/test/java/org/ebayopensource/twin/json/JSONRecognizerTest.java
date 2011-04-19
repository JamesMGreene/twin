// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.json;

import java.util.*;

import org.testng.annotations.*;
import static org.testng.AssertJUnit.*;

public class JSONRecognizerTest {
	private static class Tag implements JSONStaticRecognizer {
		String text;
		public Tag(String text) {
			this.text = text;
		}
		public String toString() {
			return "["+text+"]";
		}
		
		public boolean equals(Object other) {
			return other instanceof Tag && text.equals(((Tag)other).text);
		}
		
		@SuppressWarnings("unused")
		public static Tag recognize(Map<String,Object> data) {
			if(data.containsKey("tag"))
				return new Tag(String.valueOf(data.get("tag")));
			return null;
		}
	}
		
	@DataProvider(name="multiRecognizers")
	public Object[][] multiRecognizers() {
		String json1 = "{\"tag\": 1}";
		String json2 = "{\"tag\": 2}";
		String json3 = "{\"tag\": 3}";
		
		final Object tag1 = new Tag("tag1");
		final Object tag2 = new Tag("tag2");
		final Object tag3 = new Tag("tag3");
		
		JSONRecognizer recognize1 = new JSONRecognizer() {
			public Object recognize(Map<String, Object> jsonObject) {
				if(Integer.valueOf(1).equals(jsonObject.get("tag")))
					return tag1;
				return null;
			}
		};
		JSONRecognizer recognize2 = new JSONRecognizer() {
			public Object recognize(Map<String, Object> jsonObject) {
				if(Integer.valueOf(2).equals(jsonObject.get("tag")))
					return tag2;
				return null;
			}
		};
		JSONRecognizer recognizeAllAs3 = new JSONRecognizer() {
			public Object recognize(Map<String, Object> jsonObject) {
				if(jsonObject.containsKey("tag"))
					return tag3;
				return null;
			}
		};

		return new Object[][]{
				{"Without recognizer works", Map.class, json1, null},
				{"recognizer 1 works", tag1, json1, new JSONRecognizer[]{ recognize1 }},
				{"sole recogniser misses", Map.class, json1, new JSONRecognizer[]{ recognize2 }},
				{"first hits, second misses", tag1, json1, new JSONRecognizer[]{ recognize1, recognize2 }},
				{"first misses, second hits", tag2, json2, new JSONRecognizer[]{ recognize1, recognize2 }},
				{"both miss", Map.class, json3, new JSONRecognizer[]{ recognize1, recognize2 }},
				{"first hits, second hits", tag1, json1, new JSONRecognizer[]{ recognize1, recognizeAllAs3 }},
				{"first hits, second hits", tag3, json1, new JSONRecognizer[]{ recognizeAllAs3, recognize1 }},
		};
	}
	
	@Test(dataProvider="multiRecognizers")
	public void verifyMultipleRecognizersInvokedCorrectly(String description, Object expected, String json, JSONRecognizer[] recognizers) {
		if(recognizers == null)
			recognizers = new JSONRecognizer[0];
		Object result = JSON.decode(json, recognizers);
		if(expected instanceof Class<?>) {
			assertTrue(description+": decoded "+json+" instanceof "+((Class<?>)expected).getSimpleName(), ((Class<?>)expected).isInstance(result));
		} else {
			assertSame(description, expected, result);
		}
	}
	
	@DataProvider(name="recursiveRecognizers")
	public Object[][] recursiveRecognizers() {
		final Object tag1 = new Tag("tag1");
		final Object tag2 = new Tag("tag2");
		
		Map<String,Object> tag1map = new HashMap<String,Object>();
		tag1map.put("data", tag1);
		
		JSONRecognizer recognize1 = new JSONRecognizer() {
			public Object recognize(Map<String, Object> jsonObject) {
				if(Integer.valueOf(1).equals(jsonObject.get("tag")))
					return tag1;
				return null;
			}
		};
		JSONRecognizer recognize2 = new JSONRecognizer() {
			public Object recognize(Map<String, Object> jsonObject) {
				if(jsonObject.containsKey("data"))
					assertSame("jsonObject[data]", tag1, jsonObject.get("data"));
				if(Integer.valueOf(2).equals(jsonObject.get("tag")))
					return tag2;
				return null;
			}
		};

		return new Object[][]{
				{"works in list", Arrays.asList(tag1), "[{\"tag\":1}]", new JSONRecognizer[]{ recognize1 }},
				{"works in object", tag1map, "{\"data\":{\"tag\":1}}", new JSONRecognizer[]{ recognize1 }},
				{"outer overwrites inner", tag2, "{\"tag\":2, \"data\":{\"tag\":1}}", new JSONRecognizer[]{recognize1, recognize2}},
				{"outer overwrites inner", tag2, "{\"tag\":2, \"data\":{\"tag\":1}}", new JSONRecognizer[]{recognize2, recognize1}},
		};
	}
	
	@Test(dataProvider="recursiveRecognizers")
	public void verifyRecursiveRecognizersInvokedCorrectly(String description, Object expected, String json, JSONRecognizer[] recognizers) {
		if(recognizers == null)
			recognizers = new JSONRecognizer[0];
		Object result = JSON.decode(json, recognizers);
		if(expected instanceof Class<?>) {
			assertTrue(description+": decoded "+json+" instanceof "+((Class<?>)expected).getSimpleName(), ((Class<?>)expected).isInstance(result));
		} else {
			assertEquals(description, expected, result);
		}
	}
	
	@Test
	public void verifyStaticRecognizer() {
		assertEquals(new Tag("hello static"), JSON.decode("{\"tag\": \"hello static\"}", Tag.class));
	}
}
