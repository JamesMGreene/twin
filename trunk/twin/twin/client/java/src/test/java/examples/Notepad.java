// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package examples;
import java.io.*;
import java.net.URL;

import org.ebayopensource.twin.*;
import org.ebayopensource.twin.element.*;

public class Notepad {
	public static void pause(double seconds) {
		long millis = (long)(seconds * 1000);
		if(millis == 0)
			return;
		try { Thread.sleep(millis); } catch(Exception e) {}
	}
	
	public static void main(String[] args) throws TwinException,IOException {
		Application session = new Application(new URL("http://localhost:4444/"));
		session.open("notepad", null);
		try {
			System.out.println("Application name: "+session.getApplicationName());
			System.out.println("Application version: "+session.getApplicationVersion());
			
			// Wait for the main window to appear and grab it
			Window window = session.getWindow();
			System.out.println(window);
			
			// Dump the whole tree of the window
			System.out.println(session.getDesktop().getStructure());
			
			// Resize the window
			System.out.println("Bounds="+window.getBounds());
			window.setSize(600, 200);
			System.out.println("\tNow bounds="+window.getBounds());
			
			// Enter some dramatic text
			window.type("Hello world!\n");
			window.type("This window will self-destruct in 3...");
			pause(0.3);
			window.type("2...");
			pause(0.3);
			window.type("1...");
			pause(0.3);			
			window.type("BOOM!");

			// oops!
			window.contextMenu(100,100).item("Undo").click();
			// same thing
			window.click(100, 100, MouseButton.Right);
			Menu openMenu = session.getDesktop().waitForDescendant(Criteria.type(Menu.class), 1.0);
			MenuItem undo = openMenu.getChild(Criteria.name("Undo"));
			undo.click();
				
			// Take screenshots
			Screenshot screenshot = window.getScreenshot();
			System.out.println("Got screenshot: type="+screenshot.getContentType()+" size="+screenshot.getData().length);
			
			window.menu("File").openMenu().item("Exit").click();
		} finally {
			// kill the app
			session.close();				
		}
	}
}
