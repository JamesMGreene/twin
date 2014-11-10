// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide;

import java.awt.*;
import java.lang.reflect.InvocationTargetException;
import java.util.*;
import java.util.List;

import javax.imageio.ImageIO;
import javax.swing.SwingUtilities;

public class Utils {
	public static void setIconsOnWindow(Window window) {
		List<? extends Image> list;
		try {
			list = Arrays.asList(
					ImageIO.read(Utils.class.getResource("icons/twinlogo16.png")),
					ImageIO.read(Utils.class.getResource("icons/twinlogo32.png")),
					ImageIO.read(Utils.class.getResource("icons/twinlogo48.png")),
					ImageIO.read(Utils.class.getResource("icons/twinlogo64.png"))
				);
			window.setIconImages(list);
		} catch (Exception e) {
			e.printStackTrace();
		}
	}

	public static void runOnDispatchThread(Runnable runnable) {
		if(EventQueue.isDispatchThread()) {
			runnable.run();
		} else {
			try {
				SwingUtilities.invokeAndWait(runnable);
			} catch (InvocationTargetException e) {
				Throwable t = e.getCause();
				if(t instanceof Error)
					throw (Error)t.getCause();
				if(t instanceof RuntimeException)
					throw (RuntimeException)t.getCause();
				throw new IllegalStateException("Unchecked exception thrown unexpectedly", t);
			} catch (InterruptedException e) {
				throw new RuntimeException(e);
			}
		}
	}
}
