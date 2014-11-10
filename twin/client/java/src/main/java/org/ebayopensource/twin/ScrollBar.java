// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.awt.Rectangle;


/**
 * A ScrollBar is an element that provides a means for scrolling horizontally or vertically.
 * <p>
 * It is typically acquired with Element.getHorizontalScrollBar() and Element.getVerticalScrollBar()
 * <p>
 * Note that a ScrollBar is not an Element, this is because we are merging two concepts that UIAutomation keeps distinct: <ul>
 * <li>A scrollable item - a view that might not all fit on the screen at once, and can move around to accommodate that</li>
 * <li>A scroll-bar control - an element typically used to manipulate a scrollable item</li>
 * </ul>
 * While these two things usually go together, you can have a stand-alone scrollbar, or a scrollable item with no scrollbars. 
 * This is frequently the case when UIAutomation can't see the association between the scrollbar and the item it scrolls. Therefore 
 * we need special framework support to expose scrollbars in a consistent way.
 */
public interface ScrollBar {
	/** An axis for scrolling - horizontal or vertical */
	public enum Orientation {
		Horizontal,
		Vertical
		;
		
		/** Get the minimum value of the given rectangle on this axis */
		public double getMin(Rectangle rectangle) {
			switch(this) {
			case Horizontal:
				return rectangle.getMinX();
			case Vertical:
				return rectangle.getMinY();
			}
			throw new IllegalStateException();
		}
		/** Get the maximum value of the given rectangle on this axis */
		public double getMax(Rectangle rectangle) {
			switch(this) {
			case Horizontal:
				return rectangle.getMaxX();
			case Vertical:
				return rectangle.getMaxY();
			}
			throw new IllegalStateException();			
		}
		/** Get the center of the given rectangle on this axis */
		public double getMid(Rectangle rectangle) {
			switch(this) {
			case Horizontal:
				return rectangle.getCenterX();
			case Vertical:
				return rectangle.getCenterY();
			}
			throw new IllegalStateException();			
		}
		/** Get the size of the given rectangle on this axis */
		public double getSize(Rectangle rectangle) {
			return getMax(rectangle) - getMin(rectangle);
		}
		/** 
		 * Determine whether the 'container' rectangle contains the 'child', looking 
		 * solely on this axis. 
		 */
		public boolean contains(Rectangle container, Rectangle child) {
			return getMin(container) <= getMin(child) && getMax(container) >= getMax(child);
		}
	}
	
	/** Get the orientation of this scroll bar */
	public abstract Orientation getOrientation();

	/** 
	 * Get the current scroll position, as a number between 0.0 (left, top) and 1.0 (right, bottom).
	 * If the scroll bar has no position (because the content fits), returns 0.0
	 */
	public abstract double getPosition() throws TwinException;

	/**
	 * Set the current scroll position. 
	 * Has no effect if the content fits.
	 * @param pos a number between 0.0 (left, top) and 1.0 (right, bottom)
	 */
	public abstract void setPosition(double pos) throws TwinException;
}