// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

package org.ebayopensource.twin;

import java.lang.annotation.*;

/** 
 * This annotation marks ElementImpl methods with the ControlType or ControlPattern interfaces 
 * that they can be invoked through. This is checked at runtime, but mostly exists for documentation purposes.
 */
@Retention(RetentionPolicy.RUNTIME)
@Documented
@interface Require {
	Class<?>[] pattern() default {};
	Class<?> type() default Void.class;
}
