// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.json;

import java.io.*;
import java.lang.reflect.*;
import java.util.*;

/** 
 * Utility methods for converting Java objects to and from JSON strings.
 * <p>
 * Objects implementing the JSONable interface will be so transformed before serialization.
 * <p>
 * JSONRecognizers can be used to turn Maps into custom objects on deserialization.
 * <p>
 * Wraps the json.org library. There were some methods to convert between Java objects
 * and json.org objects, but these are now deprecated. 
 */
public class JSON {
	/** 
	 * Convert a Java object to a string using JSON serialization
	 * @throws IllegalArgumentException on serialization error
	 */
	public static String toString(Object data) {
		return encode(data);
	}
	/** 
	 * Convert a Java object to a string using JSON serialization
	 * @throws IllegalArgumentException on serialization error
	 */
	public static String encode(Object data) {
		StringWriter sw = new StringWriter();
		try {
			encode(data, sw);
		} catch (IOException e) {
			throw new RuntimeException(e);
		}
		return sw.toString();
	}
	public static void encode(Object data, OutputStream stream) throws IOException {
		Writer writer = new OutputStreamWriter(stream, "UTF-8");
		encode(data, writer);
	}
	public static void encode(Object data, Writer writer) throws IOException {
		while(data instanceof JSONable)
			data = ((JSONable)data).toJSON();
		
		if(data instanceof Double || data instanceof Short) {
			double value = ((Number)data).doubleValue();
			if(Double.isInfinite(value) || Double.isNaN(value)) // JSON can not represent these
				data = null;
			else {
				writer.write(String.valueOf(value));
				return;
			}
		}

		if(data == null) {
			writer.write("null");
			return;
		}
		
		// double and short already handled.
		if(data instanceof Number || data instanceof Boolean) {
			writer.write(data.toString());
			return;
		}
		if(data instanceof Character)
			data = data.toString();
		if(data instanceof Enum<?>)
			data = ((Enum<?>)data).name();
		
		if(data.getClass().isArray()) {
			ArrayList<Object> list = new ArrayList<Object>();
			int length = Array.getLength(data);
			for(int i=0; i<length; i++)
				list.add(Array.get(data, i));
			data = list; // continue
		}
		if(data instanceof Map<?,?>) {
			writer.write('{');
			boolean any=false;
			for(Map.Entry<?, ?> entry : ((Map<?,?>)data).entrySet()) {
				if(any)
					writer.write(',');
				encode(String.valueOf(entry.getKey()), writer);
				writer.write(':');
				encode(entry.getValue(), writer);
				any = true;
			}
			writer.write('}');
			return;
		}
		if(data instanceof List<?>) {
			writer.write('[');
			boolean any=false;
			for(Object item : (List<?>)data) {
				if(any)
					writer.write(',');
				encode(item, writer);
				any = true;
			}
			writer.write(']');
			return;
		}
		if(data instanceof String) {
			String string = (String)data;
			char[] hex = new char[]{'\\', 'u', '0', '0', '0', '0'};
			writer.write('"');
			for(int i=0; i<string.length(); i++) {
				char c = string.charAt(i);
				switch(c) {
				case '\r':
					writer.write("\\r");
					break;
				case '\n':
					writer.write("\\n");
					break;
				case '\b':
					writer.write("\\b");
					break;
				case '\f':
					writer.write("\\f");
					break;
				case '\t':
					writer.write("\\t");
					break;
				case '"':
				case '\\':
					writer.write('\\'); // fall thru
					writer.write(c);
					break;
				default:
					if(c < 0x20 || c >= 0x80) {
						hex[2] = HEX[(c >> 12)&0xf];
						hex[3] = HEX[(c >>  8)&0xf];
						hex[4] = HEX[(c >>  4)&0xf];
						hex[5] = HEX[(c >>  0)&0xf];
						writer.write(hex);
					} else {
						writer.write(c);
					}
					break;
				}
			}
			writer.write('"');
			return;
		}
		
		throw new IllegalArgumentException("Cannot encode "+data.getClass().getName()+": "+data);
	}
	private static final char[] HEX = "0123456789abcdef".toCharArray();
	
	/** 
	 * Convert a String to a Java object using JSON deserialization
	 * @throws IllegalArgumentException on deserialization error 
	 */
	public static Object decode(String text, JSONRecognizer... recognizers) {
		PushbackReader reader = new PushbackReader(new StringReader(text));
		try {
			return decode(reader, recognizers);
		} catch (IOException e) {
			throw new RuntimeException(e);
		}
	}
	public static Object decode(InputStream in, JSONRecognizer... recognizers) throws IOException {
		PushbackReader reader = new PushbackReader(new InputStreamReader(in, "UTF-8"));
		return decode(reader, recognizers);
	}
	public static Object decode(PushbackReader reader, JSONRecognizer... recognizers) throws IOException {
		int start = skipWhitespace(reader);
		switch(start) {
		case -1:
			throw new IllegalArgumentException("EOF at start of decode");
		case '"':
			return recognize(readString(reader, recognizers), recognizers);
		case '{':
			return recognize(readMap(reader, recognizers), recognizers);
		case '[':
			return recognize(readList(reader, recognizers), recognizers);
		case 't': case 'f': case 'n':
			return recognize(readTrueFalseNull(reader, recognizers), recognizers);
		case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9': case '-':
			return recognize(readNumber(reader, recognizers), recognizers);
		default:
			throw new IllegalArgumentException("Unexpected character "+(char)start+" at start of decode");
		}
	}
	private static Boolean readTrueFalseNull(PushbackReader in, JSONRecognizer[] recognizers) throws IOException {
		int[] data = new int[5];
		data[0] = in.read();
		data[1] = in.read();
		data[2] = in.read();
		data[3] = in.read();
		if(data[3] < 0)
			throw new IllegalArgumentException("End of file while reading bareword");
		
		switch(data[0]) {
		case 't':
			if(!(data[1] == 'r' && data[2] == 'u' && data[3] == 'e'))
				throw new IllegalArgumentException("Expected 'true' after reading 't'");
			return true;
		case 'n':
			if(!(data[1] == 'u' && data[2] == 'l' && data[3] == 'l'))
				throw new IllegalArgumentException("Expected 'null' after reading 'n'");
			return null;
		case 'f':
			data[4] = in.read();
			if(data[4] < 0)
				throw new IllegalArgumentException("End of file while reading bareword");
			if(!(data[1] == 'a' && data[2] == 'l' && data[3] == 's' && data[4] == 'e'))
				throw new IllegalArgumentException("Expected 'false' after reading 'f'");
			return false;
		}
		throw new IllegalStateException();
	}
	private static Number readNumber(PushbackReader in, JSONRecognizer[] recognizers) throws IOException {
		StringBuffer sb = new StringBuffer();
		boolean isDecimal=false;
		out: while(true) {
			int c = in.read();
			switch(c) {
			case -1:
				break out;
			case '.':
			case 'e': case 'E':
				isDecimal=true; // fall through
			case '-': case '+': 
			case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9':
				sb.append((char)c);
				break;
			default:
				in.unread((char)c);
				break out;
			}
		}
		if(isDecimal)
			return Double.parseDouble(sb.toString());
		long l = Long.parseLong(sb.toString());
		if(l <= Integer.MAX_VALUE && l >= Integer.MIN_VALUE)
			return (int)l;
		return l;
	}
	private static String readString(PushbackReader in, JSONRecognizer[] recognizers) throws IOException {
		int firstQuote = in.read();
		if(firstQuote != '"')
			throw new IllegalStateException();
		
		StringBuffer data = new StringBuffer();
		out: while(true) {
			int c = in.read();
			switch(c) {
			case -1:
				throw new IllegalArgumentException("String meets end of file");
			case '"':
				break out;
			case '\\':
				int d = in.read();
				switch(d) {
				case -1:
					throw new IllegalArgumentException("Escape sequence meets end of file");
				case '\\':
				case '"':
					data.append((char)d);
					break;
				case 'r':
					data.append('\r');
					break;
				case 'n':
					data.append('\n');
					break;
				case 'b':
					data.append('\b');
					break;
				case 'f':
					data.append('\f');
					break;
				case 't':
					data.append('\t');
					break;
				case 'u':
					int hex1 = in.read();
					int hex2 = in.read();
					int hex3 = in.read();
					int hex4 = in.read();
					if(hex4 < 0)
						throw new IllegalArgumentException("Unicode escape meets end of file");
					data.append((char)Integer.parseInt(""+(char)hex1+(char)hex2+(char)hex3+(char)hex4, 16));
					break;
				default:
					throw new IllegalArgumentException("Unknown escape sequence \\"+(char)d);
				}
				break;
			default:
				data.append((char)c);
				break;
			}
		}
		return data.toString();
	}
	private static Map<String, Object> readMap(PushbackReader in, JSONRecognizer... recognizers) throws IOException {
		int open = in.read();
		if(open != '{')
			throw new IllegalStateException();
		HashMap<String,Object> result = new HashMap<String, Object>();
		out: while(true) {
			int next = skipWhitespace(in);
			if(next < 0)
				throw new IllegalArgumentException("EOF inside map");
			else if(next == '}') {
				in.read();
				break out;
			}
			if(!result.isEmpty()) { 
				if(next == ',') {
					in.read();
					next = skipWhitespace(in);
				} else {
					throw new IllegalArgumentException("Expected , or } in map, got "+(char)next);
				}
			}

			if (next != '"') 
				throw new IllegalArgumentException("Expected \" to begin key in map, got "+(char)next);
				
			String key = readString(in, recognizers);
			int colon = skipWhitespace(in);
			if(colon != ':')
				throw new IllegalArgumentException("Expected : after key name in map, got "+(char)colon);
			in.read();
			skipWhitespace(in);
			result.put(key, decode(in, recognizers));
		}
		return result;
	}
	private static List<Object> readList(PushbackReader in, JSONRecognizer... recognizers) throws IOException {
		int open = in.read();
		if(open != '[')
			throw new IllegalStateException();
		ArrayList<Object> result = new ArrayList<Object>();
		out: while(true) {
			int next = skipWhitespace(in);
			if(next < 0)
				throw new IllegalArgumentException("EOF inside list");
			if(next == ']') {
				in.read();
				break out;
			}
			if(!result.isEmpty()) {
				if(next == ',') {
					in.read();
					skipWhitespace(in);
				} else {
					throw new IllegalArgumentException("Unexpected character in list "+(char)next+", expected , or ]");					
				}
			}
			result.add(decode(in, recognizers));
		}
		return result;
	}
	@SuppressWarnings("unchecked")
	private static Object recognize(Object o, JSONRecognizer... recognizers) {
		if(!(o instanceof Map<?,?>))
			return o;
		Map<String,Object> map = (Map<String,Object>)o;
		for(JSONRecognizer recognizer : recognizers) {
			Object result = recognizer.recognize(map);
			if(result != null)
				return result;
		}
		return map;
	}
	private static int skipWhitespace(PushbackReader reader) throws IOException {
		while(true) {
			int i = reader.read();
			if(i < 0)
				return i;
			else if(!Character.isWhitespace((char)i)) {
				reader.unread(i);
				return i;
			}
		}
	}
	
	/*
	public static Object decode(String text, JSONRecognizer... recognizers) {
		text = text.trim();
		try {
			if(text.startsWith("{"))
				return _wrappedToPOJO(new JSONObject(text), recognizers);
			if(text.startsWith("["))
				return _wrappedToPOJO(new JSONArray(text), recognizers);
			if(text.startsWith("\""))
				return parseString(text);
			// bug in JSON lib
			if(text.length() == 0)
				throw new IllegalArgumentException("Blank JSON text");
			return _wrappedToPOJO(JSONObject.stringToValue(text), recognizers);
		} catch (JSONException e) {
			throw new IllegalArgumentException(e);
		}
	}
	*/
	
	/** 
	 * Convert a String to a Java object using JSON deserialization
	 * @throws IllegalArgumentException on deserialization error 
	 * The method signature has two recognizer patterns so that decode(String) isn't ambiguous
	 * The classes passed must implement the JSONStaticRecognizer interface.
	 * We can't statically enforce this because you can't have varargs of generic types.
	 */
	public static Object decode(String text, Class<?> firstStaticRecognizer, Class<?>... restStaticRecognizers) {
		Class<?>[] staticRecognizers = new Class<?>[restStaticRecognizers.length+1];
		staticRecognizers[0] = firstStaticRecognizer;
		System.arraycopy(restStaticRecognizers, 0, staticRecognizers, 1, restStaticRecognizers.length);
		
		JSONRecognizer[] recognizers = new JSONRecognizer[staticRecognizers.length];
		for(int i=0; i<recognizers.length; i++) {
			final Class<?> type = staticRecognizers[i];
			if(!JSONStaticRecognizer.class.isAssignableFrom(type))
				throw new IllegalArgumentException(type.getName()+" does not implement "+JSONStaticRecognizer.class.getSimpleName());
			recognizers[i] = new JSONRecognizer() {
				private Method method;
				{
					try {
						method = type.getMethod("recognize", Map.class);
						if(!Modifier.isStatic(method.getModifiers()))
							throw new NoSuchMethodException("recognize method is not static");
					} catch (NoSuchMethodException e) {
						throw new IllegalStateException(type.getName()+" implements "+JSONStaticRecognizer.class.getSimpleName()+" but doesn't have public static Object recognize(Map<String,Object> value)");
					}
				}
				@Override
				public Object recognize(Map<String, Object> jsonObject) {
					try {
						return method.invoke(null, jsonObject);
					} catch (InvocationTargetException e) {
						throw new RuntimeException(e);
					} catch (IllegalAccessException e) {
						throw new RuntimeException(e);
					}
				}				
			};
		}
		return decode(text, recognizers);
	}
}
