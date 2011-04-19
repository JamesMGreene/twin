// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.io.*;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.*;

import org.apache.http.*;
import org.apache.http.client.*;
import org.apache.http.conn.params.*;
import org.apache.http.entity.*;
import org.apache.http.impl.client.*;
import org.apache.http.impl.conn.tsccm.ThreadSafeClientConnManager;
import org.apache.http.message.*;
import org.apache.http.params.*;

import org.ebayopensource.twin.json.*;

/** 
 * A connection to a Twin server. This is a low-level class that should not be used directly, use Application instead.
 */
class TwinConnection {
	URL url;
	public TwinConnection(URL url) {
		if(url.getPath().endsWith("/")) try {
			url = new URL(url.getProtocol(), url.getHost(), url.getPort(), url.getPath().substring(0, url.getPath().length()-1));
		} catch (MalformedURLException e) {
			throw new RuntimeException(e);
		}
		this.url = url;
	}
	/**
	 * Send an OPTIONS request to the server.
	 * @param path the URL to query
	 * @return The list of allowed HTTP methods, or an empty list if the Allow header is not present in the response.
	 * @throws TwinException
	 */
	List<String> options(String path) throws TwinException {
		try {
			BasicHttpRequest request = new BasicHttpRequest("OPTIONS", url+path);
			HttpClient client = getClient();
			HttpResponse response = client.execute(new HttpHost(url.getHost(), url.getPort()), request);
			Header hdr = response.getFirstHeader("Allow");
			if(hdr == null || hdr.getValue().isEmpty())
				return Collections.emptyList();
			return Arrays.asList(hdr.getValue().split("\\s*,\\s*"));
		} catch (IOException e) {
			throw TwinError.UnknownError.create("IOException when accessing RC", e);
		}
	}
	/** 
	 * Send a request to the server.
	 * @param method the HTTP method e.g. "GET"/"POST"/"DELETE" etc
	 * @param path the path within the server e.g. "/elements/12345"
	 * @param body the body to be encoded as JSON and included with the request, or null
	 * @param recognizers the list of JSONRecognizers to interpret the response during JSON decoding
	 * @return the decoded response as a Map (javascript object)
	 * @throws TwinException
	 */
	Map<String,Object> request(String method, String path, Map<String,Object> body, JSONRecognizer... recognizers) throws TwinException {
		try {
			return _request(method, path, body, recognizers);
		} catch (IOException e) {
			throw TwinError.UnknownError.create("IOException when accessing RC", e);
		}
	}
	
	private static ThreadSafeClientConnManager connManager;
	private static HttpParams params;
	private HttpClient getClient() {
		synchronized(TwinConnection.class) {
			if(connManager == null) {
				DefaultHttpClient client = new DefaultHttpClient();
				params = client.getParams().copy();
				params.setParameter(ConnManagerPNames.MAX_CONNECTIONS_PER_ROUTE, new ConnPerRouteBean(50));
				params.setIntParameter(ConnManagerPNames.MAX_TOTAL_CONNECTIONS, 200);
				connManager = new ThreadSafeClientConnManager(
						params,
						client.getConnectionManager().getSchemeRegistry()
				);
			}
		}
		DefaultHttpClient client = new DefaultHttpClient(connManager, params);
		client.setRedirectHandler(new DefaultRedirectHandler());				
		return client;
	}
	
	@SuppressWarnings("unchecked")
	private Map<String,Object> _request(String method, String path, Map<String,Object> body, JSONRecognizer... recognizers) throws IOException, TwinException {
		String uri = url+path;
		HttpRequest request;
		if(body == null) {
			BasicHttpRequest r = new BasicHttpRequest(method, uri);
			request = r;
		} else {
			BasicHttpEntityEnclosingRequest r = new BasicHttpEntityEnclosingRequest(method, uri);
			StringEntity entity;
			try {
				entity = new StringEntity(JSON.encode(body), "utf-8");
			} catch (UnsupportedEncodingException e) {
				throw new RuntimeException(e);
			}
			entity.setContentType("application/json; charset=utf-8");
			r.setEntity(entity);
			request = r;
		}
		
		HttpClient client = getClient();
		try {
			HttpResponse response = client.execute(new HttpHost(url.getHost(), url.getPort()), request);
			HttpEntity entity = response.getEntity();
			if(entity == null)
				return null;
			String contentType = entity.getContentType().getValue();
			boolean isJson = (contentType!=null) && ("application/json".equals(contentType) || contentType.startsWith("application/json;"));
			String result = null;
			
			InputStream in = entity.getContent();
			try {
				Reader r = new InputStreamReader(in, "UTF-8");
				StringBuilder sb = new StringBuilder();
				char[] buf = new char[256];
				int read;
				while((read=r.read(buf,0,buf.length))>=0)
					sb.append(buf,0,read);
				r.close();	
				
				result = sb.toString();
			} finally {
				try { in.close(); } catch (Exception e) {}
			}

			int code = response.getStatusLine().getStatusCode();
			if(code >= 400) {
				if(isJson) {
					try {
						throw deserializeException((Map<String,Object>)JSON.decode(result));
					} catch (IllegalArgumentException e) {
						throw TwinError.UnknownError.create("Couldn't parse error response: \n"+result, e);
					}
				}
				if(code == 404)
					throw TwinError.UnknownCommand.create("Got server response "+code+" for request "+uri);
				else 
					throw TwinError.UnknownError.create("Got server response "+code+" for request "+uri+"\nBody is "+result);
			}
			
			if(!isJson)
				throw TwinError.UnknownError.create("Got wrong content type "+contentType+" for request "+uri+"\nBody is "+result);
			
			try {
				return (Map<String,Object>)JSON.decode(result, recognizers);
			} catch (Exception e) {
				throw TwinError.UnknownError.create("Malformed JSON result for request "+uri+": \nBody is "+result, e);
			}
		} catch (ClientProtocolException e) {
			throw new IOException(e);
		}
	}
	
	/** Convert the given response object into a java exception  */
	@SuppressWarnings("unchecked")
	static TwinException deserializeException(Map<String,Object> exception) {
		TwinError responseCode = exception.containsKey("status") 
				? TwinError.get(((Number)exception.get("status")).intValue())
				: TwinError.UnknownError;
		return deserializeException(responseCode, (Map<String, Object>) exception.get("value"));
	}
	
	@SuppressWarnings("unchecked")
	private static TwinException deserializeException(TwinError responseCode, Map<String,Object> exception) {
		TwinException ex = responseCode.create((String)exception.get("message"));
		ex.className = (String)exception.get("class");
		if(exception.containsKey("stackTrace")) {
			List<?> array = (List<?>)exception.get("stackTrace");
			List<StackTraceElement> trace = new ArrayList<StackTraceElement>();
			for(Object elt : array) {
				Map<String,Object> element = (Map<String,Object>)elt;
				trace.add(new StackTraceElement(
						(String)element.get("className"),
						(String)element.get("methodName"),
						(String)element.get("fileName"),
						(element.containsKey("lineNumber") ? ((Number)element.get("lineNumber")).intValue() : -1)
				));
			}
			for(StackTraceElement elt : new Exception().getStackTrace()) {
				if(TwinConnection.class.getName().equals(elt.getClassName()))
					continue;
				if(Application.class.getName().equals(elt.getClassName()) && 
						("ensureSuccess".equals(elt.getMethodName()) || elt.getMethodName().startsWith("request")))
					continue;
				trace.add(elt);
			}
			ex.setStackTrace(trace.toArray(new StackTraceElement[trace.size()]));
		}
		if(exception.containsKey("cause") && exception.get("cause") != null)
			ex.initCause(deserializeException(TwinError.UnknownError, (Map<String, Object>) exception.get("cause")));
		
		return ex;		
	}
}
