// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.awt.image.BufferedImage;
import java.io.*;
import java.util.*;
import javax.imageio.*;

/** An image captured from the remote screen, stored in memory */
public class Screenshot {
	/** The encoded image data */
	byte[] data;
	/** The MIME type of the image data */
	String contentType;
	/** 
	 * Creates a new screenshot with the given data
	 * @param data the encoded image data
	 * @param contentType the MIME type indicating the encoding used
	 */
	public Screenshot(byte[] data, String contentType) {
		this.data = data;
		this.contentType = contentType;
	}
	/** Get the MIME type indicating the encoding used for the image data */
	public String getContentType() {
		return contentType;
	}
	/** Get the encoded image data */
	public byte[] getData() {
		return data;
	}
	
	/** 
	 * Save the screenshot in the given directory.
	 * <p>
	 * It is saved with a random UUID filename, and an extension appropriate to the image encoding. 
	 * @param directory the parent directory to save the file into
	 * @return the file that was saved
	 * @throws IOException
	 */
	public File saveIn(File directory) throws IOException {
		return save(directory, UUID.randomUUID().toString());
	}
	/** 
	 * Save the screenshot in the given directory, with the given base-name.
	 * <p>
	 * It is saved with the provided filename, and an extension appropriate to the image encoding is added. 
	 * @param directory the parent directory to save the file into
	 * @param basename the non-extension part of the filename to used
	 * @return the file that was saved
	 * @throws IOException
	 */
	public File save(File directory, String basename) throws IOException {
		File dest = new File(directory, basename + "." + getExtension(contentType));
		save(dest);
		return dest;
	}
	/**
	 * Save the screenshot to the given target file. The parent directory must exist.
	 * @param target the file to save
	 * @throws IOException
	 */
	public void save(File target) throws IOException {
		OutputStream fileOut = null;
		try {
			fileOut = new FileOutputStream(target);
			fileOut.write(data);
		} finally {
			if(fileOut != null) try { fileOut.close(); } catch (Exception ex) {}
		}
	}
	/**
	 * Get the screenshot as a BufferedImage.
	 */
	public BufferedImage getImage() {
		ByteArrayInputStream input = new ByteArrayInputStream(data);
		try {
			return ImageIO.read(input);
		} catch (IOException e) {
			throw new IllegalStateException(e);
		}
	}
	/**
	 * Get a file extension appropriate for the given MIME type.
	 * @param contentType the MIME type of the file to be saved.
	 * @return a file extension such as "png".
	 * @throws IllegalArgumentException if the MIME type is unrecognised.
	 */
	public static String getExtension(String contentType) {
		if(contentType.equalsIgnoreCase("image/png"))
			return "png";
		if(contentType.equalsIgnoreCase("image/jpeg"))
			return "jpg";
		if(contentType.equalsIgnoreCase("image/gif"))
			return "gif";
		throw new IllegalArgumentException("Unknown contentType "+contentType);
	}
}
