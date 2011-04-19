// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide.frob;

import java.awt.*;
import java.awt.event.*;
import java.lang.reflect.*;

import javax.swing.*;

import org.ebayopensource.twin.*;

interface Widget {
	public Object getValue();
	public Component getComponent();
}

@SuppressWarnings("serial")
public class DefaultFrobber extends Frobber {
	Widget[] widgets;
	Method method;
	Element element;
	
	public DefaultFrobber(Element element, Method method, String name) {
		this.method = method;
		this.element = element;
		setLayout(new BoxLayout(this, BoxLayout.X_AXIS));
		
		addWidgets();

		JButton execute = new JButton(name == null ? method.getName() : name);
		execute.addActionListener(new ActionListener(){public void actionPerformed(ActionEvent e){ frob(); }});
		add(execute);
	}
	private void addWidgets() {
		Class<?>[] paramTypes = method.getParameterTypes();
		widgets = new Widget[paramTypes.length];
		for(int i=0; i<paramTypes.length; i++) {
			widgets[i] = getWidget(paramTypes[i]);
			add(widgets[i].getComponent());
			add(Box.createRigidArea(new Dimension(5,5)));
		}
	}
	private Widget getWidget(Class<?> type) {
		if(type == Integer.TYPE) {
			final JFormattedTextField tf = new JFormattedTextField();
			tf.setValue(new Integer(0));
			return new Widget() {
				public Object getValue() {
					return ((Number)tf.getValue()).intValue();
				}
				public Component getComponent() {
					return tf;
				}
			};
		}
		if(type == String.class) {
			final JTextField tf = new JTextField();
			return new Widget() {
				public Object getValue() {
					return tf.getText();
				}
				public Component getComponent() {
					return tf;
				}
			};
		}
		if(type.isEnum()) {
			Object[] values;
			try {
				values = (Object[])type.getMethod("values").invoke(null);
			} catch (Exception e) {
				throw new IllegalStateException(e);
			}
			return new SelectorWidget(values);
		}
		if(type == Boolean.TYPE || type == Boolean.class)
			return new SelectorWidget(true,false);
		throw new IllegalStateException("Unsupported parameter type "+type.getName());
	}
	private class SelectorWidget implements Widget {
		final JComboBox combo;
		public SelectorWidget(Object... values) {
			combo = new JComboBox(values);
		}
		public Object getValue() {
			return combo.getSelectedItem();
		}
		public Component getComponent() {
			return combo;
		}
	}
	
	private void frob() {
		Object[] params = new Object[widgets.length];
		for(int i=0; i<params.length; i++)
			params[i] = widgets[i].getValue();
		try {
			Object result = method.invoke(element, params);
			if(method.getReturnType() == Void.TYPE)
				result = Void.TYPE; // denotes no result should be displayed
			sendResult(result);
		} catch (InvocationTargetException e) {
			signal(e.getCause() instanceof Exception ? ((Exception)e.getCause()) : e);
		} catch (IllegalAccessException e) {
			throw new RuntimeException(e);
		}
	}
}
