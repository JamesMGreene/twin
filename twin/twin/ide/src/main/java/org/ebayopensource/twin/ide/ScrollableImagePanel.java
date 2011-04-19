// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide;

import java.awt.*;
import java.awt.image.BufferedImage;

import javax.swing.*;

@SuppressWarnings("serial")
public class ScrollableImagePanel extends JScrollPane {
	ImagePanel panel;
	
	public ScrollableImagePanel() {
		panel = new ImagePanel();
		getViewport().add(panel);
	}
	
	public void setImage(BufferedImage image) {
		panel.setImage(image);
		getViewport().setView(panel);
	}
	
	private static class ImagePanel extends JComponent {
		BufferedImage image;
		
		public synchronized void setImage(BufferedImage image) {
			this.image = image;
		}
		
		private synchronized Dimension getImageSize() {
			if(image == null)
				return null;
			return new Dimension(image.getWidth(), image.getHeight());
		}
		Dimension empty = new Dimension(0,0);
		
		public Dimension getPreferredSize() {
			Dimension imageSize = getImageSize();
			return (imageSize == null) ? empty : imageSize;
		}

		@Override
		public Dimension getMaximumSize() {
			Dimension imageSize = getImageSize();
			return (imageSize == null) ? empty : imageSize;
		}

		@Override
		public Dimension getMinimumSize() {
			Dimension imageSize = getImageSize();
			return (imageSize == null) ? empty : imageSize;
		}

		protected synchronized void paintComponent(Graphics g) {
			g.clearRect(0, 0, getWidth(), getHeight());
			if(image != null)
				g.drawImage(image, 0, 0, null);
		}
	}
}
