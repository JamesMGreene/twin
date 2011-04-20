// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.
//

package org.ebayopensource.twin;

import java.io.*;
import java.net.*;
import java.util.*;

import org.ebayopensource.twin.json.*;
import org.ebayopensource.twin.element.*;

/**
 * A windows application controlled by a Twin automation server.
 * <p>
 * An application is created and launched using an application name (which must match that on the server), an optional version number, 
 * and other optional parameters. When an application is closed, its process will be immediately terminated if still running, so it's better to 
 * shut it down cleanly. Note that when used in unit tests, the application will be opened before your test runs and closed after your test terminates.
 * <p>
 * At any given time, and application can have zero or more windows open. These can be listed with getWindows(). For the common case of a single window 
 * that appears shortly after the application is launched, getWindow() will wait for the window to appear and return it.
 * <p>
 * Window is a subclass of Element, and contain a tree of Elements representing the contents of the window. See {@link org.ebayopensource.twin.Element Element} for details of navigating 
 * this tree. There is also a special element called Desktop that contains all windows of your application.
 * <p>
 * The client makes several requests of the server when it launches: <dl>
 * <dt>Desired capabilities</dt>
 * <dd>These are key-value pairs describing how the application must be configured on the server.
 * When you call open(String appName, String version), this creates a default set of capabilities:
 * "applicationName"=appName, "version"=version. You can specify other capabilities by using the overloaded forms of open().</dd>
 * <dt>Session setup</dt>
 * <dd>These are key-object pairs describing what  the server should do before opening your session. 
 * For example, overwriting an application config file with one provided by the client. 
 * These may be ignored by the server, unless desired capabilities specify that they must be accepted. 
 * For example: "files"=[ {"path"="C:/config.txt", "data"="Base64DataHere"} ] would request the server to overwrite C:/config.txt 
 * with the provided data before running the test. <br>
 * By convention the desired capability "sessionSetup.files" should be set to "true" 
 * to indicate that this is required. You can specify session setup data by calling addSessionSetup(String, Object). The optional 
 * third parameter can be set to true to automatically set the corresponding desired capability.</dd>
 * </dl>
 */
public class Application {
	TwinConnection connection;
	String sessionId;
	/** The session setup parameters */
	Map<String,Object> sessionSetup = new HashMap<String,Object>();
	/** The desired capabilities */
	Map<String,Object> desiredCapabilities = new HashMap<String,Object>();
	/** The actual capabilities returned by the server */
	Map<String,Object> capabilities;
	/** The default timeout e.g. for getWindow() */
	private double timeout=30.0;

	/** A recognizer that picks up objects of the form {"class":"foo", "uuid":"12345"} and wraps them in RemoteObject instances */
	private JSONRecognizer recognizeRemoteObjects = new JSONRecognizer() {
		public Object recognize(Map<String,Object> jsonObject) {
			if(jsonObject.get("class") instanceof String && jsonObject.get("uuid") instanceof String) {
				return new RemoteObject(Application.this, (String)jsonObject.get("class"), (String)jsonObject.get("uuid"), jsonObject);
			}
			return null;
		}
	};
	
	/** 
	 * Create an application proxy that will connect to the given automation server. 
	 * The application must be open()ed before being used.
	 */
	public Application(URL url) {
		connection = new TwinConnection(url);
	}	
	
	/**
	 * Configure session setup information.
	 * Also adds the capability sessionSetup.{name} to the desiredCapabilities.
	 */
	public void addSessionSetup(String name, Object value) {
		addSessionSetup(name, value, true);
	}
	/**
	 * Configure session setup information.
	 * If required is true, also adds the capability sessionSetup.{name} to the desiredCapabilities.
	 */
	public void addSessionSetup(String name, Object value, boolean required) {
		sessionSetup.put(name, value);
		desiredCapabilities.put("sessionSetup."+name, true);
	}
	
	/** 
	 * Tries to enable logging of HTTP wire traffic.
	 * This sets the commons logger to SimpleLog, and configures SimpleLog to log Apache HTTPClient wire traffic. 
	 * <p>
	 * Note that this must be called before any activity begins. It is equivalent to:
	 * <pre>
	 * System.setProperty("org.apache.commons.logging.Log", "org.apache.commons.logging.impl.SimpleLog");
	 * System.setProperty("org.apache.commons.logging.simplelog.log.org.apache.http.wire", "debug");
	 * </pre>
	 */ 
	public static void enableWireLogging() {
		System.setProperty("org.apache.commons.logging.Log", "org.apache.commons.logging.impl.SimpleLog");
		System.setProperty("org.apache.commons.logging.simplelog.log.org.apache.http.wire", "debug");
	}
	/** Get the default timeout value for methods such as getWindow() that implicitly wait. The default is 30 seconds. */
	public double getTimeout() {
		return timeout;
	}
	/** Set the default timeout value for methods such as getWindow() that implicitly wait. The default is 30 seconds. */
	public void setTimeout(double timeout) {
		this.timeout = timeout;
	}
	/** 
	 * Launch the application on the remote server 
	 * @param applicationName must match the configured application name on the remote server
	 * @param version if non-null, must match the version on the remote server
	 * @throws TwinException
	 */
	public void open(String applicationName, String version) throws TwinException {
		open(applicationName, version, null);
	}
	/** 
	 * Launch the application on the remote server, specifying extra required capabilities
	 * @param applicationName must match the configured application name on the remote server
	 * @param version if non-null, must match the version on the remote server
	 * @param otherCapabilities a map of custom capabilities, all of which must match the remote server
	 * @throws TwinException
	 */
	public void open(String applicationName, String version, Map<String,String> otherCapabilities) throws TwinException {
		Map<String,String> desiredCapabilities = otherCapabilities == null ? new HashMap<String,String>() : new HashMap<String,String>(otherCapabilities);
		desiredCapabilities.put("applicationName", applicationName);
		desiredCapabilities.put("version", version);
		open(desiredCapabilities);
	}
	/**
	 * Launch the application on the remote server, specifying all capabilities as a map. 
	 * The capabilities must include "applicationName".
	 * @param desiredCapabilities all capabilities required, must match the remote server
	 * @throws TwinException
	 */
	@SuppressWarnings("unchecked")
	public void open(Map<String,String> desiredCapabilities) throws TwinException {
		if(sessionId != null)
			throw new IllegalStateException("Session already open with id "+sessionId);
		this.desiredCapabilities.putAll(desiredCapabilities);
		Map<String,Object> request = new HashMap<String,Object>();
		request.put("desiredCapabilities", this.desiredCapabilities);
		request.put("sessionSetup", sessionSetup);
		Map<String,Object> result = connection.request("POST", "/session", request, recognizeRemoteObjects);
		ensureSuccess(result);
		if(!result.containsKey("sessionId") || !result.containsKey("value"))
			throw TwinError.UnknownError.create("Success response didn't include sessionId or value: "+result);			
		sessionId = (String)result.get("sessionId");
		capabilities = (Map<String,Object>)result.get("value");
	}
	/** 
	 * Issue a low-level OPTIONS request to the remote server.
	 * This is mainly for internal use, but can also be used to access server features that the client does not offer directly.
	 * The HTTP method is always OPTIONS, and the returned value is a list of HTTP methods that are allowed.
	 */
	public List<String> options(String path) throws TwinException {
		if(sessionId == null)
			throw new IllegalStateException("Session not open");
		while(path.startsWith("/"))
			path = path.substring(1);
		path = "/session/"+sessionId+"/"+path;
		return connection.options(path);
	}
	/**
	 * Issue a low-level request to the remote server. 
	 * This is mainly for internal use, but can also be used to access server features that the client does not offer directly.
	 * @param method the HTTP method to use - typically GET/POST/DELETE
	 * @param path the path of the resource to access (not including any prefix that is part of the endpoint URL). e.g. /elements/12345/click
	 * @param body the data to send. If non-null, this will be serialised as JSON and sent as the HTTP body. GET with body <em>is</em> supported.
	 * @return the contents of the "value" attribute of the returned and decoded JSON object. This can be a List, Map, String, Integer, Double, Boolean, RemoteObject, or null
	 * @throws TwinException
	 */
	public Object request(String method, String path, Map<String,Object> body) throws TwinException {
		if(sessionId == null)
			throw new IllegalStateException("Session not open");
		while(path.startsWith("/"))
			path = path.substring(1);
		path = "/session/"+sessionId+"/"+path;
		Map<String,Object> jsonResult = connection.request(method, path, body, recognizeRemoteObjects);
		ensureSuccess(jsonResult);
		if(!jsonResult.containsKey("value"))
			throw TwinError.UnknownError.create("Got success response with no value set: \n"+jsonResult);
		return jsonResult.get("value");
	}
	/**
	 * Issue a low-level request to the remote server.
	 * This is identical to request(method, path, body), but the result is expected to be a Map (i.e. javascript object) and an exception is thrown if not.
	 * @throws TwinException
	 */
	@SuppressWarnings("unchecked")
	public Map<String,Object> requestObject(String method, String path, Map<String,Object> body) throws TwinException {
		Object result = request(method, path, body);
		if(!(result instanceof Map<?,?>))
			throw new TwinException("Expected object from "+method+" "+path+" but got "+result);
		return (Map<String,Object>)result;
	}
	/**
	 * Issue a low-level request to the remote server.
	 * This is identical to request(method, path, body), but the result is expected to be a List (i.e. javascript array) and an exception is thrown if not.
	 * @throws TwinException
	 */
	@SuppressWarnings("unchecked")
	public List<Object> requestArray(String method, String path, Map<String,Object> body) throws TwinException {
		Object result = request(method, path, body);
		if(!(result instanceof List<?>))
			throw new TwinException("Expected object from "+method+" "+path+" but got "+result);
		return (List<Object>)result;
	}
	/**
	 * Tells the remote server to forcibly close the application and end the session. 
	 * After this call the application can no longer be used.
	 * @throws TwinException
	 */
	public void close() throws TwinException {
		if(sessionId == null)
			throw new IllegalStateException("Session not open");
		Map<String,Object> result = connection.request("DELETE", "/session/"+sessionId, null);
		ensureSuccess(result);
		sessionId = null;
	}
	/** Throw an appropriate exception if the result object does not represent a success */
	private void ensureSuccess(Map<String,Object> result) throws TwinException {
		TwinError code = result.containsKey("status") ? 
			TwinError.get(((Number)result.get("status")).intValue())
			: TwinError.UnknownError;
		if(code != TwinError.Success)
			throw TwinConnection.deserializeException(result);
	}
	
	/** The desktop instance */
	private Desktop desktop;
	/**
	 * The desktop is a special root element containing all of this Application's windows. 
	 * Taking screenshots of the desktop will capture the whole screen (including other running applications).
	 * @return the Desktop instance
	 * @throws TwinException
	 */
	public Desktop getDesktop() throws TwinException {
		if(desktop == null) // don't need to lock, if two accidentally get created it doesn't matter
			desktop = new DesktopImpl(this);
		return desktop;
	}
	
	/** The clipboard instance */
	private Clipboard clipboard;
	public Clipboard getClipboard() throws TwinException {
		if(clipboard == null) // don't need to lock, if two accidentally get created it doesn't matter
			clipboard = new Clipboard(this);
		return clipboard;
	}
	
	/** 
	 * Get the application name, as returned by the server. 
	 * @throws TwinException
	 */
	public String getApplicationName() throws TwinException {
		return (String)capabilities.get("applicationName");
	}
	/** 
	 * Get the application version, as returned by the server. This may be null if not configured.
	 * @throws TwinException
	 */
	public String getApplicationVersion() throws TwinException {
		return (String)capabilities.get("version");
	}
	/**
	 * Get the full set of capabilities, as returned by the server.
	 * @throws TwinException
	 */
	public Map<String,Object> getCapabilities() throws TwinException {
		return Collections.unmodifiableMap(capabilities);
	}
	/**
	 * Gets the only window this application has open.
	 * <p>
	 * If the application has no window open, it waits up to the default timeout period for one to appear. 
	 * If the application has several windows open, an exception is thrown. 
	 * <p>
	 * For finer control, use getWindows().
	 * @return the single window owned by the application
	 * @throws TwinException
	 */
	public Window getWindow() throws TwinException {
		return (Window)getDesktop().waitForChild(Criteria.type(Window.class), timeout);
	}
	/**
	 * Get all windows this application has open.
	 * @return the list of top-level windows this application has open.
	 * @throws TwinException
	 */
	public List<Window> getWindows() throws TwinException {
		List<Element> elements = getDesktop().getChildren(Criteria.type(Window.class));
		List<Window> windows = new ArrayList<Window>();
		for(Element e : elements) 
			windows.add((Window)e);
		return windows;
	}
	/**
	 * Get the currently open menu. 
	 * <p>
	 * Note that multiple menus can be open at once, such as with a parent menu and a submenu. 
	 * This method returns the first open menu found.
	 * 
	 * @return the currently open menu, or null if none is found after 1 second.
	 * @throws TwinException
	 */
	public Menu getOpenMenu() throws TwinException {
		try {
			return (Menu)getDesktop().waitForDescendant(Criteria.type(Menu.class), 1);
		} catch (TwinNoSuchElementException e) {
			return null;
		}
	}
	/**
	 * Get the currently focused element.
	 * @return the element that is currently focused for keyboard input, or null if none.
	 * @throws TwinException
	 */
	public Element getFocusedElement() throws TwinException {
		return ElementImpl.create((RemoteObject)request("GET", "/element/active", null));
	}
	/**
	 * Capture an image of the whole screen. 
	 * This is a convenience method for getDesktop().getScreenshot()
	 * @return a screenshot of the whole screen
	 * @throws TwinException
	 */
	public Screenshot getScreenshot() throws TwinException {
		return getDesktop().getScreenshot();
	}
	
	/**
	 * Upload a file to the host machine
	 * @return an Attachment object that represents the remote file
	 * @throws TwinException, IOException
	 */
	public Attachment upload(File f) throws TwinException, IOException {
		HashMap<String,Object> body = new HashMap<String,Object>();
		body.put("data", Attachment.getBase64Contents(f));
		body.put("name", f.getName());
		RemoteObject remote = (RemoteObject)request("POST", "/attachment", body);
		return new Attachment(remote);
	}
	
	/** 
	 * Upload a file to the host machine with the given data and filename
	 * @param filename the local file name. The remote server should attempt to preserve the file extension.
	 * @return an Attachment object that represents the remote file
	 * @throws TwinException, IOException
	 */
	public Attachment upload(InputStream stream, String filename) throws TwinException, IOException {
		HashMap<String,Object> body = new HashMap<String,Object>();
		body.put("data", Attachment.getBase64Contents(stream));
		if(filename != null)
			body.put("name", filename);
		RemoteObject remote = (RemoteObject)request("POST", "/attachment", body);
		return new Attachment(remote);
	}
	
	/**
	 * Upload a file to the host machine with the given data
	 * @return an Attachment object that represents the remote file
	 * @throws TwinException, IOException
	 */
	public Attachment upload(InputStream stream) throws TwinException, IOException {
		return upload(stream, null);
	}

	/**
	 * Upload a file to the host machine from the given resource.
	 * The resource is evaluated relative to the given class, and the file extension is preserved.
	 * E.g. if you pass in resource="bar.csv", context=com.ebay.package.Foo.class, 
	 * then the uploaded resource will be "/com/ebay/package/bar.csv" and given a name ending in ".csv". 
	 * If context is null, the resource will be forced to be absolute.
	 * @throws IOException 
	 * @throws TwinException 
	 */
	public Attachment upload(String resource, Class<?> context) throws TwinException, IOException {
		if(context == null) {
			context = Application.class;
			if(!resource.startsWith("/"))
				resource = "/"+resource;
		}
		return upload(context.getResourceAsStream(resource), resource);
	}
	
	/**
	 * Upload a file to the host machine from the given resource.
	 * The resource reference will be treated as absolute.
	 * @throws IOException 
	 * @throws TwinException 
	 */
	public Attachment upload(String resource) throws TwinException, IOException {
		return upload(resource, null);
	}
}
