// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide.frob;

import java.awt.*;
import java.lang.reflect.*;
import java.util.*;
import java.util.List;

import javax.swing.*;

import org.ebayopensource.twin.Element;

@SuppressWarnings("serial")
public class AttributeFrobber extends Frobber {
	public AttributeFrobber(Element element, Method m, String name) {
		try {
			JLabel label = new JLabel(name);
			JLabel value = new JLabel(string(m.invoke(element)));
			value.setFont(value.getFont().deriveFont(Font.PLAIN));
			setLayout(new BoxLayout(this, BoxLayout.X_AXIS));
			add(label);
			add(Box.createHorizontalStrut(10));
			add(value);
			add(Box.createHorizontalGlue());
		} catch (InvocationTargetException e) {
			Throwable t = e.getCause();
			if(t instanceof Error)
				throw (Error)t;
			if(t instanceof RuntimeException)
				throw (RuntimeException)t;
			throw new RuntimeException(t);
		} catch (IllegalAccessException e) {
			throw new RuntimeException(e);
		}
	}
	
	private String string(Object o) {
		if(o == null)
			return null;
		
		if(o instanceof Dimension) {
			Dimension d = (Dimension)o;
			return d.width + " x " + d.height;
		}
		
		if(o instanceof Point) {
			Point p = (Point)o;
			return p.x + ", " + p.y;
		}
		
		if(o instanceof Class<?>)
			return ((Class<?>)o).getSimpleName();
		
		if(o.getClass().isArray() && !o.getClass().getComponentType().isPrimitive())
			return string(Arrays.asList((Object[])o));
		
		if(o instanceof Collection<?>) {
			List<String> result = new ArrayList<String>();
			for(Object x : (Collection<?>)o)
				result.add(string(x));
			return result.toString();
		}
		
		return o.toString();
	}
}
