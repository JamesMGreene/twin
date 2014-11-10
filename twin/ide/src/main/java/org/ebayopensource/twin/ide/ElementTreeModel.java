// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide;

import java.awt.Component;
import java.net.URL;
import java.util.*;

import javax.swing.*;
import javax.swing.event.*;
import javax.swing.tree.*;

import org.ebayopensource.twin.*;

public class ElementTreeModel implements TreeModel, ResultConsumer {
	private WeakHashMap<Element,List<Element>> childCache = new WeakHashMap<Element, List<Element>>();
	private WeakHashMap<Element,Element> parents = new WeakHashMap<Element, Element>();
	
	private synchronized List<Element> getChildren(Element element) {
		if(!childCache.containsKey(element)) try {
			childCache.put(element, element.getChildren());
			for(Element child : childCache.get(element))
				parents.put(child, element);
		} catch (TwinException e) {
			signal(e);
			childCache.put(element, Collections.<Element>emptyList());
		}
		return childCache.get(element);
	}
	private synchronized Element getParent(Element element) throws TwinException {
		if(!parents.containsKey(element))
			parents.put(element, element.getParent());
		return parents.get(element);
	}
	public synchronized void clearCache(Element element, boolean force) throws TwinException {
		childCache.remove(element);
		Element cachedParent = force ? parents.remove(element) : parents.get(element);
		try {
			TreeModelEvent e = new TreeModelEvent(this, getPath(element, !force));
			for(TreeModelListener listener : listeners)
				listener.treeStructureChanged(e);
		} catch (TwinNoSuchElementException ex) {
			if(cachedParent != null)
				clearCache(cachedParent, force);
			throw ex; 
			// if the above fails, we want its exception instead.
			// the result is we throw the exception corresponding to the outermost nested element
		}
	}
	
	Application app;
	public ElementTreeModel(Application app) {
		this.app = app;
	}
	public Object getRoot() {
		return app.getDesktop();
	}
	public Object getChild(Object parent, int index) {
		return getChildren((Element)parent).get(index);
	}

	public int getChildCount(Object parent) {
		return getChildren((Element)parent).size();
	}

	public boolean isLeaf(Object node) {
		return false;
	}

	public void valueForPathChanged(TreePath path, Object newValue) {
	}

	public int getIndexOfChild(Object parent, Object child) {
		return getChildren((Element)parent).indexOf(child);
	}
	
	private List<TreeModelListener> listeners = new ArrayList<TreeModelListener>();
	public void addTreeModelListener(TreeModelListener l) {
		listeners.add(l);
	}
	public void removeTreeModelListener(TreeModelListener l) {
		listeners.remove(l);
	}
	
	public TreePath getPath(Element elt, boolean useCache) throws TwinException {
		if(elt.equals(getRoot()))
			return new TreePath(elt);

		Element parent = useCache ? getParent(elt) : elt.getParent();
		if(getChildren(parent).indexOf(elt) < 0)
			clearCache(parent, false);
		return getPath(parent, useCache).pathByAddingChild(elt);
	}
	
	private ResultConsumer consumer;
	public void setConsumer(ResultConsumer ex) {
		consumer = ex;
	}
	public void signal(Throwable e) {
		if(consumer != null)
			consumer.signal(e);
	}
	public void sendResult(Object o) {
		if(consumer != null)
			consumer.sendResult(o);
	}

	@SuppressWarnings("serial")
	public static class CellRenderer extends DefaultTreeCellRenderer {
		public Component getTreeCellRendererComponent(JTree tree, Object value,
				boolean selected, boolean expanded, boolean leaf, int row,
				boolean hasFocus) {
			
			super.getTreeCellRendererComponent(tree, value, selected, expanded, leaf, row, hasFocus);
			
			Element element = (Element)value;
			
			Icon icon = getIcon(element);
			if(icon != null)
				this.setIcon(icon);
			this.setToolTipText(element.getControlType().getSimpleName());
			this.setText(toString(element));
			
			return this;
		}
		
		private Map<String,Icon> icons = new HashMap<String,Icon>();
		private synchronized Icon getIcon(Element element) {
			String name = element.getControlType().getSimpleName();
			if(!icons.containsKey(name)) {
				URL resource = ElementTreeModel.class.getResource("icons/"+name+".png");
				if(resource == null)
					System.err.println("Icon not found for control type: "+name);
				Icon icon = resource == null ? null : new ImageIcon(resource);
				icons.put(name, icon);
			}
			return icons.get(name);
		}
		
		private String toString(Element element) {
			String name = element.getCachedName();
			if(name != null)
				return name;
			name = element.getId();
			if(name != null)
				return name;		
			name = element.getClassName();
			if(name != null)
				return name;
			return element.getControlType().getSimpleName();
		} 
	}
}
