// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.io.*;
import java.util.HashMap;

import org.apache.commons.codec.binary.*;

/**
 * An Attachment represents a temporary file uploaded to the remote machine that can be used in a test.
 * <p>
 * Suppose you are testing a spreadsheet application, and you want to open a known spreadsheet file. 
 * You store the spreadsheet locally, and send it to the remote machine as an attachment. This saves it on the remote 
 * disk and your spreadsheet application can open it.
 * <p>
 * Attachments are created via one of the Application.upload methods, which upload a local file, resource, or stream 
 * to the remote machine. You can then find the remote file path of the object with getFile(), or replace its contents.
 * <p>
 * For our spreadsheet example:
 * <pre>
 * Application app; // our spreadsheet application
 * // Show the "File Open" dialog here
 * Edit filenameInput; // the filename input box
 * 
 * Attachment spreadsheet = app.upload(new File("C:/temp.csv")); // upload the attachment to the remote system
 * String remoteName = spreadsheet.getFile(); // something like "C:/temp/1234.csv"
 * filenameInput.type(remoteName);
 * </pre>
 * 
 * @see Application#upload(File)
 * @see Application#upload(String)
 * @see Application#upload(InputStream, String)
 */
public class Attachment extends RemoteResource {
	private String path;
	private boolean deleted = false;
	
	protected Attachment(RemoteObject remote) {
		super(remote);
		this.path = (String)remote.properties.get("path");
	}

	/** WARNING: this is not the path to the file, this is internal.
	 * @see Attachment#getFile() */
	protected String getPath() {
		return "/attachment/"+remote.uuid;
	}
	
	/** 
	 * Replace the contents of the remote file with data from the given stream. 
	 * The stream is not closed afterward.
	 * @see Application#upload(InputStream)
	 */
	public void setContents(InputStream data) throws IOException, TwinException {
		if(deleted)
			throw new IllegalStateException("Attachment already deleted!");
		HashMap<String,Object> body = new HashMap<String,Object>();
		body.put("data", getBase64Contents(data));
		remote.session.request("POST", getPath(), body);
	}
	
	/**
	 * Replace the contents of the remote file with data from the given file.
	 * @see Application#upload(File)
	 */
	public void setContents(File f) throws IOException, TwinException {
		if(deleted)
			throw new IllegalStateException("Attachment already deleted!");
		HashMap<String,Object> body = new HashMap<String,Object>();
		body.put("data", getBase64Contents(f));
		remote.session.request("POST", getPath(), body);
	}
	
	/**
	 * Replace the contents of the remote file with data from the given resource (evaluated relative to the given class).
	 * @see Application#upload(String, Class)
	 */
	public void setContents(String resource, Class<?> context) throws IOException, TwinException {
		if(context == null) {
			context = Attachment.class;
			if(!resource.startsWith("/"))
				resource = "/"+resource;
		}
		setContents(context.getResourceAsStream(resource));
	}
	
	/**
	 * Replace the contents of the remote file with data from the given resource (treated as an absolute path)
	 */
	public void setContents(String resource) throws IOException, TwinException {
		setContents(resource, null);
	}
	
	/**
	 * Delete the remote file. 
	 * <p>
	 * If this method is never called, it will be deleted when the session ends.
	 */
	public void delete() throws TwinException {
		if(deleted)
			throw new IllegalStateException("Attachment already deleted!");
		remote.session.request("DELETE", getPath(), null);
		deleted = true;
	}

	static String getBase64Contents(File f) throws IOException {
		InputStream in = new FileInputStream(f);
		try {
			return getBase64Contents(in);
		} finally {
			in.close();
		}
	}
	static String getBase64Contents(InputStream in) throws IOException {
		ByteArrayOutputStream bytes = new ByteArrayOutputStream();
		Base64OutputStream out = new Base64OutputStream(bytes);

		byte[] buf = new byte[1024];
		int read;
		while((read=in.read(buf))>=0)
			out.write(buf, 0, read);
		out.close();
		
		return bytes.toString("UTF-8");
	}
	
	/**
	 * Return the path to the file on the remote machine, e.g. "C:/temp/1234.png"
	 */
	public String getFile() {
		if(deleted)
			throw new IllegalStateException("Attachment already deleted!");
		return path;
	}	
	/**
	 * Return the path to the file on the remote machine, e.g. "C:/temp/1234.png"
	 */
	public String toString() {
		return getFile();
	}
}
