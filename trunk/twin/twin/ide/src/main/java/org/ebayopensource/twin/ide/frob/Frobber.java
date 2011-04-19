// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin.ide.frob;

import javax.swing.JPanel;

import org.ebayopensource.twin.ide.ResultConsumer;

@SuppressWarnings("serial")
public class Frobber extends JPanel implements ResultConsumer {
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
}
