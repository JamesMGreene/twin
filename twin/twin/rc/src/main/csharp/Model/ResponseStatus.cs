// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Twin.Model {
    enum ResponseStatus {
		Success = 0,
		NoSuchElement = 1,
		NoSuchFrame = 2,
		UnknownCommand = 9,
		StaleElementReference = 10,
		ElementNotVisible = 11,
		InvalidElementState = 12,
		UnknownError = 13,
		ElementNotSelectable = 14,
		XPathLookupError = 19,
		NoSuchWindow = 13,
		InvalidCookieDomain = 24,
		CannotSetCookie = 25,
		
		// Twin specific
		NoSuchSession = 100,
    }
}
