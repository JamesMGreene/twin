// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide;

import java.awt.*;
import java.awt.event.*;
import java.net.*;

import javax.swing.*;
import javax.swing.event.*;
import javax.swing.tree.TreePath;

import org.ebayopensource.twin.*;
import org.ebayopensource.twin.ide.frob.ElementFrobPanel;

@SuppressWarnings("serial")
public class SessionConsole extends JPanel implements ResultConsumer {
	Application app;
	JTree navigator;
	ElementTreeModel navModel;
	ScrollableImagePanel screenshot;
	ElementFrobPanel elementFrob;
	
	public SessionConsole(Application session) {
		app = session;
		navModel = new ElementTreeModel(app);
		navModel.setConsumer(this);
		navigator = new JTree(navModel);
		navigator.setCellRenderer(new ElementTreeModel.CellRenderer());
		screenshot = new ScrollableImagePanel();
		elementFrob = new ElementFrobPanel();
		elementFrob.setConsumer(this);
		navigator.addTreeSelectionListener(new TreeSelectionListener() {
			public void valueChanged(TreeSelectionEvent e) {
				Object target = navigator.getLastSelectedPathComponent();
				if(target instanceof Element)
					select((Element)target, false);
			}
		});
		navigator.addKeyListener(new KeyAdapter() {
			public void keyPressed(KeyEvent e) {
				if(e.getKeyCode() == KeyEvent.VK_F5) {
					refresh();
				}
			}
		});
		
		setLayout(new BorderLayout());
		JSplitPane navContentSplit = new JSplitPane(JSplitPane.HORIZONTAL_SPLIT);
		JSplitPane screenshotFrobSplit = new JSplitPane(JSplitPane.HORIZONTAL_SPLIT);
		screenshotFrobSplit.setResizeWeight(1);
		navigator.setPreferredSize(new Dimension(250,0));
		setPreferredSize(new Dimension(1000, 600));
		add(navContentSplit);
		
		navContentSplit.setLeftComponent(new JScrollPane(navigator, JScrollPane.VERTICAL_SCROLLBAR_AS_NEEDED, JScrollPane.HORIZONTAL_SCROLLBAR_NEVER));
		navContentSplit.setRightComponent(screenshotFrobSplit);
		screenshotFrobSplit.setLeftComponent(screenshot);
		screenshotFrobSplit.setRightComponent(new JScrollPane(elementFrob, JScrollPane.VERTICAL_SCROLLBAR_ALWAYS, JScrollPane.HORIZONTAL_SCROLLBAR_NEVER));			
	}
	private void refresh() {
		Object target = navigator.getLastSelectedPathComponent();
		if(target instanceof Element)
			select((Element)target, true);		
	}
	private void select(Element e, boolean refresh) {
		try {
			if(refresh)
				navModel.clearCache(e, false);
			Screenshot shot = e.getScreenshot();
			screenshot.setImage(shot.getImage());
			TreePath path = navModel.getPath(e, true);
			elementFrob.setElement(e);
			navigator.setSelectionPath(path);
			navigator.expandPath(path);
		} catch (TwinException ex) {
			try {
				navModel.clearCache(e, true);
			} catch (TwinNoSuchElementException ex2) {}
			signal(ex);
		}		
	}
	
	public void close() {
		try {
			app.close();
		} catch (Exception e) {
			System.err.println("Error closing connection");
			e.printStackTrace();
		}
	}
	
	public static void main(String[] args) throws Exception {
		JFrame jf = new JFrame("IDE");
		Utils.setIconsOnWindow(jf);
		Application app = new Application(new URL("http://localhost:4444/"));
		app.open("notepad", null);
		try {
			final SessionConsole sc = new SessionConsole(app);
			jf.add(sc);
			jf.pack();
			jf.addWindowListener(new WindowAdapter() {
				@Override
				public void windowClosing(WindowEvent e) {
					sc.close();
				}
			});
			jf.setDefaultCloseOperation(JFrame.DISPOSE_ON_CLOSE);
			jf.setVisible(true);
		} catch (Exception e) {
			app.close();
			throw e;
		}
	}

	private static final int EXCEPTION_COOLDOWN_MS = 100;
	private long displayExceptionsAfter=0;
	public synchronized void signal(Throwable t) {
		t.printStackTrace();
		if(t.getMessage() != null && t.getMessage().contains("STA thread"))
			t = t.getCause();
		final Throwable tt = t;
		if(System.currentTimeMillis() > displayExceptionsAfter) {
			SwingUtilities.invokeLater(new Runnable() { public void run() {
				JOptionPane.showMessageDialog(SessionConsole.this, tt, "Error occurred", JOptionPane.ERROR_MESSAGE);
			}});
			displayExceptionsAfter = System.currentTimeMillis() + EXCEPTION_COOLDOWN_MS;
		}
	}
	public void sendResult(final Object o) {
		if(o instanceof Element) {
			select((Element)o, false);
		} else {
			refresh();
			if (o != Void.TYPE) { // Void.TYPE denotes empty result
				SwingUtilities.invokeLater(new Runnable() { public void run() {
					JOptionPane.showMessageDialog(SessionConsole.this, o, "Result", JOptionPane.INFORMATION_MESSAGE);
				}});
			}
		}
	}
}
