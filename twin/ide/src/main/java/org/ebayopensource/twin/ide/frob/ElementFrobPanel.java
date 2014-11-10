// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide.frob;

import java.awt.*;
import java.lang.reflect.*;
import java.util.*;
import java.util.List;

import javax.swing.*;
import javax.swing.border.*;

import org.ebayopensource.twin.*;
import org.ebayopensource.twin.ide.*;

@SuppressWarnings("serial")
public class ElementFrobPanel extends Box implements ResultConsumer {
	Box box;
	public ElementFrobPanel() {
		super(BoxLayout.Y_AXIS);
		box = this;
	}
	Element element;

	public synchronized void setElement(Element element) throws TwinException {
		this.element = element;
		updateContents();
	}
	
	private void updateContents() throws TwinException {
		try {
			Utils.runOnDispatchThread(new Runnable() { public void run() {
				final List<ElementTypeFrobPanel> etfps = createPanels(element);
				box.removeAll();
				for(ElementTypeFrobPanel etfp : etfps)
					box.add(etfp);
				box.add(new Box.Filler(new Dimension(0,0),
						new Dimension(0,0),
						new Dimension(Short.MAX_VALUE,Short.MAX_VALUE)));
				revalidate();
			}});
		} catch (Exception e) {
			if(e instanceof TwinException)
				throw (TwinException)e;
			throw new RuntimeException(e);
		}
	}
	
	public Dimension getPreferredSize() {
		return new Dimension(250, super.getPreferredSize().height);
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

	private List<ElementTypeFrobPanel> createPanels(Element e) {
		List<ElementTypeFrobPanel> panels = new ArrayList<ElementTypeFrobPanel>();
		panels.add(new ElementTypeFrobPanel(e, Element.class));
		panels.add(new ElementTypeFrobPanel(e, e.getControlType()));
		for(Class<?> controlPattern : e.getControlPatterns())
			panels.add(new ElementTypeFrobPanel(e, controlPattern));		
		return panels;
	}
	private class ElementTypeFrobPanel extends JPanel {
		Class<?> type;
		Element element;
		public ElementTypeFrobPanel(Element e, Class<?> type) {
			this.type = type;
			this.element = e;
			setBorder(new TitledBorder(type.getSimpleName()));
			setLayout(new BoxLayout(this, BoxLayout.Y_AXIS));
			addFrobbers();
			// for some reason 0,0 or 1,0 as min size means it only takes up the right half of the space...
			JComponent glue = new Box.Filler(new Dimension(2,1), new Dimension(1, 1), new Dimension(Short.MAX_VALUE, 1));
			add(glue);
		}
		
		private void addFrobbers() {
			JComponent actionFrobbers = new WrappingPanel(250);
			Box attributeFrobbers = Box.createVerticalBox();
			for(Method m : type.getDeclaredMethods()) {
				IDE ide = m.getAnnotation(IDE.class);
				if(ide == null)
					continue;
				
				String name = ide.name();
				if(name.isEmpty()) {
					name = m.getName();
					if(ide.attribute()) {
						if(name.startsWith("is"))
							name = name.substring(2);
						else if(name.startsWith("get"))
							name = name.substring(3);
					}
					if(name.length() > 0)
						name = name.substring(0,1).toUpperCase() + name.substring(1);
				}
				
				Frobber frobber;
				if(ide.attribute())
					frobber = new AttributeFrobber(element, m, name);
				else
					frobber = new DefaultFrobber(element, m, name);
				
				frobber.setConsumer(ElementFrobPanel.this);
				if(ide.attribute()) {
					frobber.setMaximumSize(new Dimension(Short.MAX_VALUE, frobber.getPreferredSize().height));
					attributeFrobbers.add(frobber);
					attributeFrobbers.add(spacer());
				} else if(m.getParameterTypes().length == 0) { // command
					actionFrobbers.add(frobber);
				} else {
					frobber.setMaximumSize(new Dimension(Short.MAX_VALUE, frobber.getPreferredSize().height));
					addS(frobber);
				}
			}
			if(actionFrobbers.getComponentCount() > 0) {
				addS(actionFrobbers, 0);
			}
			if(attributeFrobbers.getComponentCount() > 0) {
				addS(attributeFrobbers, 0);
			}
		}
		
		private void addS(JComponent c, int i) {
			add(spacer(), i);
			add(c, i);
		}
		private void addS(JComponent c) {
			add(c);
			add(spacer());
		}
		
		private Component spacer() {
			return Box.createRigidArea(new Dimension(5,5));
		}
	}
}
