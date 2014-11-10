// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide;

import java.awt.Dimension;
import java.awt.event.*;
import java.io.*;
import java.net.*;
import java.util.*;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import javax.swing.event.*;
import javax.swing.table.*;

import org.ebayopensource.twin.json.JSON;
import org.ebayopensource.twin.Application;

@SuppressWarnings("serial")
public class RCBrowser extends JPanel {
	private List<ActionListener> connectListeners = new ArrayList<ActionListener>();
	public void addConnectListener(ActionListener listener) {
		connectListeners.add(listener);
	}
	public void removeConnectListener(ActionListener listener) {
		connectListeners.remove(listener);
	}
	
	public class BrowserTableModel extends AbstractTableModel {
		@Override
		public int getRowCount() {
			return configurations.size();
		}

		@Override
		public String getColumnName(int column) {
			switch(column) {
			case 0:
				return "ID";
			case 1:
				return "Application Name";
			case 2:
				return "Path";
			}
			throw new IllegalStateException();
		}

		@Override
		public int getColumnCount() {
			return 3;
		}

		@Override
		public Object getValueAt(int rowIndex, int columnIndex) {
			Map<String,Object> config = configurations.get(rowIndex);
			switch(columnIndex) {
			case -1:
				return config;
			case 0:
				return config.get("id");
			case 1:
				return ((Map<?, ?>) config.get("capabilities")).get("applicationName");
			case 2:	
				return config.get("path");
			}
			throw new IllegalStateException();
		}
	}
	private URL rc;
	private List<Map<String,Object>> configurations;
	
	private JTable table;
	
	public RCBrowser(URL rc) {
		this.rc = rc;
		this.configurations = getConfigurations();
		this.table = new JTable(new BrowserTableModel());
		this.table.getTableHeader().setVisible(true);
		
		setPreferredSize(new Dimension(640, 480));
		this.setBorder(new EmptyBorder(10, 10, 10, 10));
		setLayout(new BoxLayout(this, BoxLayout.Y_AXIS));
		JLabel configsAvailable = new JLabel("Configurations available at "+rc);
		configsAvailable.setAlignmentX(0.5f);
		add(configsAvailable);
		add(Box.createVerticalStrut(10));
		add(new JScrollPane(table, JScrollPane.VERTICAL_SCROLLBAR_AS_NEEDED, JScrollPane.HORIZONTAL_SCROLLBAR_NEVER));
		
		final JButton connect = new JButton("Connect", (Icon) UIManager.get("OptionPane.informationIcon"));
		connect.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent e) {
				connectPressed();
			}
		});
		table.getSelectionModel().addListSelectionListener(new ListSelectionListener() {
			@Override
			public void valueChanged(ListSelectionEvent e) {
				connect.setEnabled(table.getSelectedRow() >= 0);
			}
		});
		table.addMouseListener(new MouseAdapter(){
			public void mouseClicked(MouseEvent e) {
				if(e.getClickCount() == 2)
					connectPressed();
			}
		});
		connect.setEnabled(false);
		connect.setAlignmentX(0.5f);
		add(connect);
	}
	
	@SuppressWarnings("unchecked")
	private void connectPressed() {
		int rowIndex = table.getSelectedRow();
		if(rowIndex < 0)
			return;
		Map<String,Object> config = (Map<String, Object>) table.getModel().getValueAt(rowIndex, -1);
		
		ActionEvent evt = new ActionEvent(config, ActionEvent.ACTION_PERFORMED, "connect");
		for(ActionListener listener : connectListeners)
			listener.actionPerformed(evt);
	}

	@SuppressWarnings("unchecked")
	private List<Map<String,Object>> getConfigurations() {
		try {
			URL statusURL = new URL(rc, "status");
			StringBuffer data = new StringBuffer();
			InputStream stream = statusURL.openStream();
			Reader reader = new InputStreamReader(stream, "UTF-8");
			char[] buf = new char[256];
			int read;
			while((read = reader.read(buf, 0, 256))>=0)
				data.append(buf, 0, read);
			Map<?,?> result = (Map<?,?>)JSON.decode(data.toString());
			stream.close();
			return (List<Map<String,Object>>)result.get("configurations");
		} catch (IOException e) {
			throw new RuntimeException(e);
		}
	}
	
	@SuppressWarnings("unchecked")
	public static void main(String[] args) throws Exception {
		URL host = new URL("http://localhost:4444");
		JFrame jf = new JFrame(host.toExternalForm());
		Utils.setIconsOnWindow(jf);
		RCBrowser browser = new RCBrowser(host);
		jf.add(browser);
		jf.pack();
		jf.setDefaultCloseOperation(JFrame.DISPOSE_ON_CLOSE);
		
		browser.addConnectListener(new ActionListener(){
			public void actionPerformed(ActionEvent e) {
				Map<String,Object> config = (Map<String,Object>)e.getSource();
				Map<String,String> capabilities = (Map<String,String>)config.get("capabilities");
				
				JFrame jf = new JFrame(capabilities.get("applicationName"));
				Utils.setIconsOnWindow(jf);
				Application app=null;
				try {
					app = new Application(new URL(capabilities.get("rc")));
					app.open(capabilities);
					final SessionConsole sc = new SessionConsole(app);
					jf.add(sc);
					jf.pack();
					jf.addWindowListener(new WindowAdapter() {
						public void windowClosing(WindowEvent e) {
							sc.close();
						}
					});
					jf.setDefaultCloseOperation(JFrame.DISPOSE_ON_CLOSE);
					jf.setVisible(true);
				} catch (Exception re) {
					try {
						if(app != null)
							app.close();
					} catch (Exception ex) { }
					
					re.printStackTrace();
					JOptionPane.showMessageDialog(jf, re, "Failed to connect", JOptionPane.ERROR_MESSAGE);
				}
			}			
		});
		
		jf.setVisible(true);
	}
	
	public static class Applet extends javax.swing.JApplet {
		public Applet() {
			setLayout(new BoxLayout(getContentPane(), BoxLayout.Y_AXIS));
		}
		
		public void init() {
			
			try {
				final URL url = this.getCodeBase();

				SecurityManager sm = System.getSecurityManager();
				if(sm != null) try {
					sm.checkConnect(url.getHost(), url.getPort());
				} catch (SecurityException e) {
					e.printStackTrace();
					JLabel error = new JLabel("Permission denied to connect to "+url+". Should the applet be signed?");
					error.setAlignmentX(0.5f);
					add(error);
					return;
				}
				
				RCBrowser browser = new RCBrowser(url);
				browser.addConnectListener(new ActionListener() {
					@SuppressWarnings("unchecked")
					public void actionPerformed(ActionEvent e) {
						Map<String,Object> config = (Map<String,Object>)e.getSource();
						Map<String,String> capabilities = (Map<String,String>)config.get("capabilities");
						
						JFrame jf = new JFrame(capabilities.get("applicationName"));
						Utils.setIconsOnWindow(jf);
						Application app=null;
						try {
							app = new Application(url);
							app.open(capabilities);
							final SessionConsole sc = new SessionConsole(app);
							jf.add(sc);
							jf.pack();
							jf.addWindowListener(new WindowAdapter() {
								public void windowClosing(WindowEvent e) {
									sc.close();
								}
							});
							jf.setDefaultCloseOperation(JFrame.DISPOSE_ON_CLOSE);
							jf.setVisible(true);
						} catch (Exception re) {
							try {
								if(app != null)
									app.close();
							} catch (Exception ex) { }
							re.printStackTrace();
							JOptionPane.showMessageDialog(jf, re, "Failed to connect", JOptionPane.ERROR_MESSAGE);
						}
					}
				});
				add(browser);
			} catch (Exception e) {
				getAppletContext().showStatus(e.toString());
				throw new RuntimeException("Error in init", e);
			}
		}
	}
}
