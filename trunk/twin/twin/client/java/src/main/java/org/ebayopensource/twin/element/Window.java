// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.element;

import org.ebayopensource.twin.*;
import org.ebayopensource.twin.pattern.*;

/**
 * A top-level or dialog window of an application.
 * <p>
 * Windows that are recognizably dialogs are usually children of a parent window. Others are usually children of the desktop.
 */
public interface Window extends ControlType, Transformable {
	/** Get the MenuItem in this window that has the given name, such as "File" */
	public MenuItem menu(String name) throws TwinException;
	/** Attempt to close the window. This may have no effect if the application ignores window close messages. */
	@IDE public void close() throws TwinException;
	/** Determine whether the window is maximized. */
	@IDE(attribute=true) public boolean isMaximized() throws TwinException;
	/** Determine whether the window is minimized.  */
	@IDE(attribute=true) public boolean isMinimized() throws TwinException;
	/** Maximise the window. */
	@IDE public void maximize() throws TwinException;
	/** Minimize the window. */
	@IDE public void minimize() throws TwinException;
	/** Restore the window (unmaximize, unminimize). */
	@IDE public void restore();
}
