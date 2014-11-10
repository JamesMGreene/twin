// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide;

import java.awt.*;

import javax.swing.*;

public class WrappingPanel extends JPanel {
	int vgap,hgap;
	int defaultSize;
	public WrappingPanel(int defaultSize) {
		this.defaultSize = defaultSize;
		FlowLayout layout = new FlowLayout();
		setLayout(layout);
		vgap = layout.getVgap();
		hgap = layout.getHgap();
	}
	
	public Dimension getMinimumSize() {
		return new Dimension(1, getHeight(getUsableWidth()));
	}
	public Dimension getMaximumSize() {
		return new Dimension(Short.MAX_VALUE, getHeight(getUsableWidth()));
	}
	public Dimension getPreferredSize() {
		return new Dimension(Short.MAX_VALUE, getHeight(getUsableWidth()));
	}
	public void setBounds(int x,int y,int w,int h) {
		super.setBounds(x,y,w,h);
		if(h != getHeight(w)) {
			SwingUtilities.invokeLater(new Runnable() { public void run() { 
				getParent().invalidate(); getParent().validate(); 
			} });
		}
	}
	
	private int getUsableWidth() {
		for(Component c = this; c != null; c = c.getParent()) {
			int w = c.getWidth();
			if(w > 0)
				return w;
		}
		return defaultSize;
	}

	int lastWidth = -1;
	int lastHeight = -1;
	private int getHeight(int width) {
		if(width == lastWidth) // cache
			return lastHeight;
		
		Component[] components = getComponents();
		if(components.length == 0)
			return 0;
		Insets insets = getInsets();
		
		int componentIndex = 0;
		int previousRowsHeight=hgap;
		int thisRowHeight=0;
		int thisRowWidth=0;
		
		while(componentIndex < components.length) {
			Dimension componentSize = components[componentIndex++].getPreferredSize();
			if(componentSize.width + hgap + thisRowWidth > width - hgap) {
				previousRowsHeight += thisRowHeight + vgap;
				thisRowWidth = -hgap;
				thisRowHeight = 0;
			}
			thisRowWidth += hgap + componentSize.width;
			thisRowHeight = Math.max(thisRowHeight, componentSize.height);
		}
		previousRowsHeight += vgap;

		lastWidth=width;
		lastHeight=previousRowsHeight + thisRowHeight + insets.top + insets.bottom;		
		return lastHeight;
	}
}
