// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.awt.Point;
import java.awt.Rectangle;
import java.awt.Dimension;

import java.util.*;

import org.ebayopensource.twin.element.*;
import org.ebayopensource.twin.pattern.ControlPattern;

/**
 * A graphical element of a Windows application.
 * <p>
 * Graphical elements of Windows applications are modeled as a tree of elements. The root is the desktop element, 
 * its children are windows, which in turn contain nested collections of panes, text fields, scroll bars, etc.
 * <p>
 * Elements returned from Twin (e.g. from Application.getWindow(), Element.getDescendants()) have the following properties:
 * <ul>
 * 		<li>implements Element</li>
 * 		<li>implements one subinterface of ControlType, indicating the abstract type of the element (e.g. Button). You can obtain this ControlType via Element.getControlType()</li>
 * 		<li>directly implements zero or more subinterfaces of ControlPattern, indicating behaviours that the element supports (e.g. Editable). You can query these ControlPatterns via Element.getControlPatterns() and Element.is()</li>
 * </ul>
 * The ControlPatterns are dynamically present or absent depending on the actual element - e.g. one TreeItem may be Expandable but another may not. 
 * Note that for ease of use in the common case, TreeItem includes Expandable so you can call treeItem.expand() without having to cast. 
 * In this case, because your TreeItem <i>statically</i> implements Expandable but at runtime doesn't support it, an exception will be thrown.
 * If you wish to check if an element supports a pattern, you should use Element.is() rather than instanceof.
 * <p>
 * The element tree is based on that of UIAutomation, and is browseable using UISpy from Microsoft. 
 * In addition, calling getStructure() on any Element will give an XML representation of the tree rooted at that element.
 * <p>
 * The tree can be searched using {@link org.ebayopensource.twin.Criteria Criteria}, which can filter by properties like ControlType. Searching is done server side, so it's reasonably
 * fast.
 */
public interface Element {
	/**
	 * Get the ControlType subinterface representing the type of this control
	 */
	@IDE(attribute=true) public Class<? extends ControlType> getControlType();
	/**
	 * Get the ControlPattern subinterfaces representing the patterns supported by this element
	 */
	@IDE(attribute=true) public List<Class<? extends ControlPattern>> getControlPatterns();
	/**
	 * Test whether this instance supports the given control pattern or control type.
	 * <p>
	 * This is preferable to instanceof as it correctly handles the case where X is of 
	 * control type T, and T extends control pattern P (because most Ts support P) but X
	 * does not support P.
	 */
	public boolean is(Class<? extends Element> patternOrType);
	/**
	 * Return this element as the given control pattern or type, throwing a ClassCastException 
	 * if it is not of the right type.
	 * <p>
	 * This is preferable to casting as it correctly handles the case where X is of 
	 * control type T, and T extends control pattern P (because most Ts support P) but X
	 * does not support P.
	 */
	public <T extends Element> T as(Class<T> patternOrType) throws ClassCastException;
	
	/**
	 * Get the containing application
	 */
	public Application getApplication();
	
	/**
	 * Take a screenshot of this element. This method asks the element to draw itself into the screenshot.
	 * In some cases children may not be drawn. For a more accurate image of what's on the screen, see getBoundsScreenshot()
	 * @return an in-memory screenshot of the element.
	 */
	public Screenshot getScreenshot() throws TwinException;
	/**
	 * Take a screenshot of an area of this element. This method asks the element to draw itself into the screenshot.
	 * In some cases children may not be drawn. For a more accurate image of what's on the screen, see getBoundsScreenshot()
	 * @return an in-memory screenshot of the element.
	 */
	public Screenshot getScreenshot(Rectangle bounds) throws TwinException;
	/**
	 * Take a screenshot of the desktop, cropped to the bounds of this element.
	 * This is a convenience method for getApplication().getDesktop().getScreenshot(getBounds())
	 * @return an in-memory screenshot of the element's region
	 */
	public Screenshot getBoundsScreenshot() throws TwinException;
	
	/** 
	 * Get an XML dump of the tree rooted at this element.
	 * <p>
	 * Equivalent to getStructure(false).
	 * @see #getStructure(boolean)
	 */
	@IDE public String getStructure() throws TwinException;
	
	/** 
	 * Get an XML dump of the tree rooted at this element.
	 * <p>
	 * This structure is generated server side, so it is much faster than traversing the Element tree locally.
	 * @param verbose if true, elements have 'patterns' and 'properties' attributes describing the remote .NET AutomationElement. 
	 * @return a string containing XML. Elements are represented by XML elements with names corresponding to their ControlType, 
	 * which contain XML elements representing their children. 
	 */
	public String getStructure(boolean verbose) throws TwinException;
	
	/**
	 * Get a list of all the element's descendants matching the given criteria.
	 */
	public <T extends Element> List<T> getDescendants(Criteria criteria) throws TwinException;
	/**
	 * Get a list of all the element's immediate children.
	 */
	public List<Element> getChildren() throws TwinException;
	/**
	 * Get a list of all the element's immediate children matching the given criteria.
	 */
	public <T extends Element> List<T> getChildren(Criteria criteria) throws TwinException;
	/**
	 * Get the single descendant of the element that matches the given criteria.
	 * @return the child that matches
	 * @throws TwinNoSuchElementException if no children match
	 * @throws TwinException if multiple children match
	 */
	public <T extends Element> T getDescendant(Criteria criteria) throws TwinException;
	/**
	 * Get the single immediate child of the element that matches the given criteria.
	 * @return the child that matches
	 * @throws TwinNoSuchElementException if no children match
	 * @throws TwinException if multiple children match
	 */
	public <T extends Element> T getChild(Criteria criteria) throws TwinException;
	/**
	 * Get the single descendant of the element that matches the given criteria, waiting up to the 
	 * application's default timeout period for it to appear.
	 * @return the descendant that matches
	 * @throws TwinNoSuchElementException if no child matches before the timeout expires
	 * @throws TwinException if multiple descendants match
	 */
	public <T extends Element> T waitForDescendant(Criteria criteria) throws TwinException;
	/**
	 * Get the single descendant of the element that matches the given criteria, waiting up to the 
	 * given duration for it to appear.
	 * @param timeout the duration to wait if no matches are immediately available (can be infinite or zero)
	 * @return the descendant that matches
	 * @throws TwinNoSuchElementException if no child matches before the timeout expires
	 * @throws TwinException if multiple descendants match
	 */
	public <T extends Element> T waitForDescendant(Criteria criteria, double timeout) throws TwinException;
	/**
	 * Get the single immediate child of the element that matches the given criteria, waiting up to the 
	 * application's default timeout period for it to appear.
	 * @return the child that matches
	 * @throws TwinNoSuchElementException if no child matches before the timeout expires
	 * @throws TwinException if multiple children match
	 */
	public <T extends Element> T waitForChild(Criteria criteria) throws TwinException;
	/**
	 * Get the single immediate child of the element that matches the given criteria, waiting up to the given duration for it to appear.
	 * @param timeout the duration to wait if no matches are immediately available (can be infinite or zero)
	 * @return the child that matches
	 * @throws TwinNoSuchElementException if no child matches before the timeout expires
	 * @throws TwinException if multiple children match
	 */
	public <T extends Element> T waitForChild(Criteria criteria, double timeout) throws TwinException;
	/**
	 * Get all 'closest' descendants of the element that match the given criteria. 
	 * <p>
	 * Closeness is measured by the number of parent-child links between this element and the descendant.
	 * This is useful for things like scrollbars or menus, where the closest elements are those corresponding to the outer control.
	 * @return the descendants that matches the given criteria and are closest to this element
	 */
	public <T extends Element> List<T> getClosestDescendants(Criteria criteria) throws TwinException;
	/**
	 * Get all 'closest' descendants of the element that match the given criteria. If none are present, waits up to the 
	 * application default timeout period for them to appear.
	 * <p>
	 * Closeness is measured by the number of parent-child links between this element and the descendant.
	 * This is useful for things like scrollbars or menus, where the closest elements are those corresponding to the outer control.
	 * @return the descendants that matches the given criteria and are closest to this element
	 * @throws TwinException if the timeout expires without a match being found
	 */
	public <T extends Element> List<T>  waitForClosestDescendants(Criteria criteria) throws TwinException;
	/**
	 * Get all 'closest' descendants of the element that match the given criteria. If none are present, waits up to the 
	 * specified timeout period for them to appear.
	 * <p>
	 * Closeness is measured by the number of parent-child links between this element and the descendant.
	 * This is useful for things like scrollbars or menus, where the closest elements are those corresponding to the outer control.
	 * @param timeout the duration to wait if no matches are immediately available (can be infinite or zero)
	 * @return the descendants that matches the given criteria and are closest to this element
	 * @throws TwinException if the timeout expires without a match being found
	 */
	public <T extends Element> List<T>  waitForClosestDescendants(Criteria criteria, double timeout) throws TwinException;
	/** Get the cached parent of this element. */
	public Element getCachedParent();
	/**
	 * Get the element's parent element. If the element has no parent (i.e. is the desktop), returns null.
	 */
	public Element getParent() throws TwinException;

	/** Get the cached name of this element. */
	public String getCachedName();
	/** Get the name of this element. This is commonly the displayed text, if the element displays text. */
	@IDE(attribute=true) public String getName() throws TwinException;
	/** Get the id of this element. */
	@IDE(attribute=true) public String getId() throws TwinException;
	/** Get the win32 class name of this element. */
	@IDE(attribute=true) public String getClassName() throws TwinException;

	/** Find out if the element is enabled or disabled. Disabled elements are typically grayed out */
	@IDE(attribute=true) public boolean isEnabled() throws TwinException;
	
	/** Get the size of the element */
	@IDE(attribute=true) public Dimension getSize() throws TwinException;
	/** Get the location (top-left corner) of the element on the screen */
	@IDE(attribute=true) public Point getLocation() throws TwinException;
	/** Get the bounds of the element on the screen */
	public Rectangle getBounds() throws TwinException;

	/** Focus this element for keyboard input */
	@IDE public void focus() throws TwinException;

	/** 
	 * Click on, or otherwise activate, the element.
	 * <p>
	 * This will first attempt to perform some sort of 'abstract click' if one is available. (e.g. Invoke, ExpandCollapse etc patterns). 
	 * This means that in most cases the element does not need to be visible, it could be e.g. scrolled out of view. 
	 * To force a physical click, use click(MouseButton).
	 * <p>
	 * If no 'abstract click' is available, a single left-click will be performed on the element's clickable-point. If the element 
	 * doesn't have a clickable-point defined, the center is used.
	 */
	@IDE public void click() throws TwinException;
	/**
	 * Click on the element with the specified button.
	 * <p>
	 * A single click will be performed on the element's clickable-point. If the element doesn't define a clickable-point, the center 
	 * is used.
	 * @param button the button to click with
	 */
	public void click(MouseButton button) throws TwinException;
	/** 
	 * Click on the element at the given point with the left mouse button.
	 * Co-ordinates are element-relative.
	 */
	public void click(int x, int y) throws TwinException;
	/**
	 * Click on the element at the given point with the given mouse button.
	 * Co-ordinates are element-relative.
	 * @param button the button to click with
	 */
	@IDE public void click(int x, int y, MouseButton button) throws TwinException;
	
	/**
	 * Focus the element and type the given text into it.
	 * The text is formatted per the .NET SendKeys API.
	 * @param text the text to enter, possibly containing codes such as {ENTER}
	 * @see Element#type(String) type()
	 * @see <a href="http://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys.send.aspx">Description of formatting codes (MSDN)</a>
	 */
	@IDE public void sendKeys(String text) throws TwinException;
	/**
	 * Focus the element and type the given text into it. 
	 * @param text the text to enter
	 * @see Element#sendKeys(String) sendKeys()
	 */
	@IDE public void type(String text) throws TwinException;
	
	/** Right click on the element, wait 1 second for a menu to appear, and return the menu.
	 * @throws TwinNoSuchElementException if no context menu appears
	 */	
	@IDE public Menu contextMenu() throws TwinException;
	/** 
	 * Right click on the element at the specified point, wait 1 second for a menu to appear, and return the menu. 
	 * Co-ordinates are element-relative.
	 * @throws TwinNoSuchElementException if no context menu appears
	 */
	public Menu contextMenu(int x, int y) throws TwinException;
	
	/** 
	 * Get the scroll bar of the given orientation most likely to control this element.
	 * This is more reliable than searching for elements of ControlType ScrollBar.
	 * <p>
	 * The tree is searched in layers, until one of the following is found:<ul>
	 * <li>An element that has ScrollPattern (highest priority)</li>
	 * <li>An element of type ScrollBar</li>
	 * <li>An element of type Pane and className ScrollBar - a Win32 standalone scrollbar control (lowest priority)</li>
	 * </ul>
	 * @param orientation the desired orientation of scrollbar
	 * @return a scrollbar with the given orientation
	 * @throws TwinNoSuchElementException if the scrollbar doesn't exist
	 */
	public ScrollBar getScrollBar(ScrollBar.Orientation orientation) throws TwinException;
	/** 
	 * Get the vertical scroll bar most likely to control this element. 
	 * This is more reliable than searching for elements of ControlType ScrollBar.
	 * @return a scrollbar with Vertical orientation
	 * @throws TwinNoSuchElementException if the scrollbar doesn't exist
	 */
	public ScrollBar getVerticalScrollBar() throws TwinException;
	/** 
	 * Get the horizontal scroll bar most likely to control this element. 
	 * This is more reliable than searching for elements of ControlType ScrollBar.
	 * @return a scrollbar with Horizontal orientation
	 * @throws TwinNoSuchElementException if the scrollbar doesn't exist
	 */
	public ScrollBar getHorizontalScrollBar() throws TwinException;
	
	/**
	 * Scroll within this element so that the specified child is completely visible.
	 * Both horizontal and vertical axis will be scrolled. If you have a vertical list of items that 
	 * may be too wide for their container, use scrollVisible(child, ScrollBar.Orientation.Vertical).
	 * Note that the child may not be visible afterwards, e.g. if this element is itself scrolled offscreen in a scrollable parent element.
	 * @param child The child to make visible within the parent.
	 * @throws TwinNoSuchElementException if a scrollbar doesn't exist (for an axis that needs to be scrolled)
	 * @throws TwinException if the element does not respond to scrollbars or is too big.
	 */
	public void scrollVisible(Element child) throws TwinException;
	/**
	 * Scroll within this element along a single axis so that the specified child is visible (with respect to that axis).
	 * The child may still not be scrolled visible on other axes.
	 * Note that the child may not be visible afterwards if this element is itself scrolled offscreen in a scrollable parent element.
	 * @param child The child to make visible within the parent.
	 * @param orientation the axis to scroll
	 * @throws TwinNoSuchElementException if a scrollbar doesn't exist (and we need to scroll on this axis)
	 * @throws TwinException if the element does not respond to the scrollbar or is too big.
	 */
	public void scrollVisible(Element child, ScrollBar.Orientation orientation) throws TwinException;
	
	/** 
	 * Returns the contained button with the given name.
	 * This is a convenience method for this.getClosestDescendants(Criteria.type(ControlType.Button).and(Criteria.name(name)))).get(0);
	 * @return the button
	 * @throws TwinNoSuchElementException if the button was not found
	 * @throws TwinException if multiple matching buttons were found at the same nesting depth
	 * TODO should this method really exist?
	 */
	public Element button(String name) throws TwinException;
	
	/**
	 * Determines whether this element still exists on the server
	 */
	@IDE(attribute=true) public boolean exists() throws TwinException;
	
	/** Wait the default timeout duration for this element to disappear, then return.
	 * @throws TwinInvalidElementStateException if the element still exists after the default timeout
	 */
	public void waitForNotExists() throws TwinException;
	
	/** Wait the specified timeout for this element to disappear, then return.
	 * @throws TwinInvalidElementStateException if the element still exists after the specified timeout
	 */ 
	public void waitForNotExists(double timeout) throws TwinException;
}
