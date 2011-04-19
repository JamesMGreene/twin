// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

// JavaScript client library
// Requires node.js, seq module (npm install seq)

var featureSignifiers = {
	"commons":"module",
	"browser":"window",
	"node":"process",
	"console":"console",
	"json":"JSON",
}
var env = {};
for(var feature in featureSignifiers) { env[feature] = eval("typeof("+featureSignifiers[feature]+")=='object'"); }
var twin;

// include(script)
if(env['node']) {
	twin = module.exports;
	twin.json = JSON;
	require.paths.unshift('.');
	twin.url = require('url');
	twin.http = require('http');
	twin.seq = require('seq');
} else {
	Twin = twin = {};
}

// log(text) - debug message
if(env['console']) {
	twin.log=function(text) { console.log(text); }
} else if(env['browser']) {
	twin.log = function(text) { window.alert(text); }
} else { 
	twin.log = function(text) {} 
}

if(!env['node'])
	twin.log("Warning: running outside of node.js not yet supported");

twin.Remote = {
	toString: function() {
		return this.uuid+" as "+this.class;
	},
};
twin.Resource = {
	__proto__: twin.Remote,
	path: function() {
		throw new Error("Resource should override path");
	},
	request: function(method, path, data, callback) {
		path = this.path() + path;
		this.application.request(method, path, data, callback);
	},
};
twin.Clipboard = {
	__proto__: twin.Resource,
	path: function() {
		return "/clipboard";
	},
	clear: function(cb) {
		this.application.request("DELETE", this.path(), null, cb);
	},
	text: function(cb) {
		this.application.request("GET", this.path(), null, function(err, data) {
			if(err)
				return cb(err);
			if(data.type == "text")
				return cb(null, data.text);
			return cb(null, null);
		});
	},
	setText: function(text, cb) {
		this.application.request("POST", this.path(), {type:'text', text:text}, cb);
	},
};
twin.Element = {
	__proto__: twin.Resource,
	path: function() {
		return "/element/"+this.uuid;
	},
	child: function(criteria, callback) {
		this._getElement("/children", criteria, null, null, true, twin.Element._single(callback));
	},
	children: function() { // [criteria], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var criteria = args.shift();
		this._getElements("/children", criteria, null, null, false, callback);
	},
	descendant: function(criteria, callback) {
		this._getElements("/descendants", criteria, null, null, true, twin.Element._single(callback));
	},
	descendants: function(criteria, callback) {
		this._getElements("/descendants", criteria, null, null, false, callback);
	},
	waitForChild: function() { // criteria, [timeout], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var criteria = args.shift();
		var timeout = args.shift() || this.application.timeout;
		this._getElements("/children", criteria, null, timeout, true, twin.Element._single(callback));
	},
	waitForDescendant: function() { // criteria, [timeout], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var criteria = args.shift();
		var timeout = args.shift() || this.application.timeout;
		this._getElements("/children", criteria, null, timeout, true, twin.Element._single(callback));
	},
	closestDescendants: function(criteria, callback) { 
		this._getElements("/descendants", criteria, 1, null, false, callback);
	},
	waitForClosestDescendants: function() { // criteria, [timeout], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var criteria = args.shift();
		var timeout = args.shift() || this.application.timeout;
		this._getElements("/descendants", criteria, 1, timeout, true, callback);
	},
	structure: function() { // [verbose], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var verbose = args.shift();
		var data = {verbose: (verbose==true)};	
		this.request("GET", "/structure", data, callback);
	},
	screenshot: function() { // [bounds], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var bounds = args.shift();
		this.request("GET", "/screenshot", bounds, function(err, result) {
			if(!err)
				result.data = new Buffer(result.data, 'base64');
			callback(err, result);
		});
	},
	boundsScreenshot: function(callback) {
		this.bounds(function(bounds) {
			this.application.desktop.screenshot(bounds, callback);
		});
	},
	click: function() { // ['left'|'middle'|'right', [x, y]], callback
		var args = Array.prototype.slice.apply(arguments);
		var callback = args.pop();	
		var button = args.shift();
		var x = args.shift();
		var y = args.shift();
		var data = {};
		if(button)
			data['button'] = button;
		if(x || y) {
			data['x'] = x;
			data['y'] = y;
		}
		this.request("POST", "/click", data, callback);
	},
	bounds: function(callback) {
		this.request("GET", "/bounds", null, callback);
	},
	enabled: function(callback) {
		this.request("GET", "/enabled", null, callback);
	},
	name: function(callback) {
		var elt = this;
		this.request("GET", "/name", null, function(err, name) {
			if(!err) {
				elt.name = name;
			}
			callback(err, name);
		});
	},
	type: function(text, callback) {
		text = text.replace(/([\+\^\%\(\)\{\}\[\]])/g, '{$1}');
		text = text.replace('\n', '~');
		text = text.replace('\b', '{BS}');
		text = text.replace('\t', '{TAB}');
		this.sendKeys(text, callback);
	},
	sendKeys: function(keys, callback) {
		this.request("POST", "/keyboard", {keys:keys}, callback);
	},
	_wrap: function(resource) {
		resource.__proto__ = twin.Element;
		return resource;
	},
	_getElements: function(subpath, criteria, count, timeout, shouldThrow, callback) {
		var elt = this;

		var data = {};
		if(criteria)
			data['criteria'] = criteria;
		if(count)
			data['count'] = count;
		if(timeout) {
			if(isFinite(timeout))
				data['waitForResults'] = timeout;
			else
				data['waitForResults'];
		}
		this.request('GET', subpath, data, function(err, results) {
			if(err) {
				callback(err);
			} else {
				if(results.length == 0 && shouldThrow) {
					callback(new Error("No results matching "+criteria.toString()));
				} else {
					var wrapped = [];
					results.forEach(function(x) {
						wrapped.push(twin.Element._wrap(x));
					});
					callback(null, wrapped);
				} 
			}
		});
	},
	_single: function(cb) {
		return function(err, data) {
			if(err)
				return cb(err);
			if(typeof(data) !== 'object' || typeof(data.length) !== 'number' || data.length != 1)
				return cb(new Error("Expected array with 1 result but got "+twin.json.stringify(data)));
			return cb(null, data[0]);
		}
	},
	toString: function(){
		return this.controlType+" name="+this.name+" id="+this.id+" @ "+this.path();
	},
};
twin.Desktop = {
	__proto__: twin.Element,
	uuid: "DESKTOP",
	class: "DESKTOP",
	path: function() {
		return "/desktop";
	},
	controlType: 'Desktop',
	name: 'Desktop',
	id: 'desktop',
};

twin.Application = function(url) {
	this.url = twin.url.parse(url);
	this.desiredCapabilities = {};
	this.sessionSetup = {};
	this.debug = false;
	this.timeout = 10;
	this.desktop = {
		__proto__: twin.Desktop,
		application: this,
	};
	this.clipboard = {
		__proto__: twin.Clipboard,
		application: this,
	};
} 

twin.Criteria = {
	Criterion: {
		and: function(arg) {
			return twin.Criteria.and(this, arg);
		},
		or: function(arg) {
			return twin.Criteria.or(this, arg);
		},
		not: function() {
			return twin.Criteria.not(this);
		},
		toString: function() {
			if(this.type == 'property')
				return this.name + "=" + this.value;
			if(this.type == 'not')
				return "not "+this.target.toString();
			var text = "";
			for(var i=0; i<this.target.length; i++) {
				if(i>0)
					text += " "+this.type+" ";
				text += this.target[i].toString();
			}
			return text;
		},
	},

	name: function(value) {
		return twin.Criteria.equal('name', value);
	},
	controlType: function(value) {
		return twin.Criteria.equal('controlType', value);
	},
	controlPattern: function(value) {
		return twin.Criteria.equal('controlPattern', value);
	},
	className: function(value) {
		return twin.Criteria.equal('className', value);
	},
	id: function(value) {
		return twin.Criteria.equal('id', value);
	},
	enabled: function(value) {
		return twin.Criteria.equal('enabled', value);
	},
	value: function(value) {
		return twin.Criteria.equal('value', value);
	},
	equal: function(name, value) {
		return {
			type: 'property',
			name: name,
			value: value,
			__proto__: twin.Criteria.Criterion,
		};
	},
	and: function() {
		return {
			type: 'and',
			target: Array.prototype.slice.apply(arguments),
			__proto__: twin.Criteria.Criterion,
		};
	},
	or: function() {
		return {
			type: 'or',
			target: Array.prototype.slice.apply(arguments),
			__proto__: twin.Criteria.Criterion,
		};
	},
	not: function(arg) {
		return {
			type: 'not',
			target: arg,
			__proto__: twin.Criteria.Criterion,
		};
	},
}

twin.Application.prototype = {
	open: function(callback) {
		var request = {
			desiredCapabilities: this.desiredCapabilities,
			sessionSetup: this.sessionSetup,
		};
		var app = this;
		var response = this._request('POST', '/session', request, function(err, response) {
			if(err)
				return callback(err);
			try {
				app.validate(response);
				app.sessionId = response.sessionId;
				app.capabilities = response.value;
				callback();
			} catch (x) {
				callback(x);
			}
		});
	},
	close: function(callback) {
		if(!callback)
			callback = function(){}
		this.request('DELETE', '', null, callback);
	},
	request: function(method, path, data, callback) {
		var app=this;
		this._request(method, '/session/' + this.sessionId + path, data, function(err, response) {
			if(err) {
				callback(err);
			} else {
				callback(null, response && response.value);
			}
		});
	},
	revive: function(key, value) {
		var app = this;
		if(value && typeof(value) == "object") {
			if(typeof(value.class)=="string" && typeof(value.uuid)=="string") {
				value.application = app;
				value.__proto__ = twin.Remote;
			}
		}
		return value;
	},
	_request: function(method, path, data, callback) {
		var app = this;
		var url = twin.url.parse(this.url.href + path);
		var dataType = data ? "application/json" : null;
		data = twin.json.stringify(data);
		this.__request(method, url, data, dataType, function(err, status, headers, output){
			if(err) {
				callback(err);
			} else {
				var isJson = headers['content-type'] && (headers['content-type'] == 'application/json' || headers['content-type'].match('^application/json;'))
				if(isJson) {
					try {
						var data = twin.json.parse(output, function(key,value) { return app.revive(key,value); });
					} catch (x) {
						return callback(new Error("Failed to parse response as JSON"));
					}
					if(status >= 400)
						return callback(app.decodeException(data));
					return callback(null, data); // TODO check for bad decode, bad status etc
				} else {
					if(status >= 400)
						return callback(new Error("HTTP error. Response code: "+status+" for "+url.href));
					return callback(new Error("Response is not JSON. Content type is "+headers['content-type']));
				}
			}
		});
	},
	__request: function(method, url, data, dataType, callback) {
		var app=this;
		this.___request(method, url, data, dataType, function(err, status, headers, data) {
			if(err)
				return callback(err);
			else {
				if(status == 301 || status == 302 || status == 303 || status == 307) {
					var newurl = twin.url.parse(twin.url.resolve(url.href, headers['location']));
					app.__request('GET', newurl, null, null, callback);
				} else {
					callback(null, status, headers, data);
				}
			}
		});
	},
	___request: function(method, url, data, dataType, callback) {
		var app = this;
		if(app.debug)
			twin.log(method+">>>"+twin.url.format(url)+">>> "+data);

		var client = twin.http.createClient(url.port, url.hostname);

		var headers = {};
		headers['Host'] = (url.port == 80) ? url.hostname : (url.hostname + ":" + url.port);
		if(dataType) {
			headers['Content-Type'] = dataType;
			headers['Transfer-Encoding'] = 'chunked';
		}

		var request = client.request(method, url.pathname, headers, null);
		if(data)
			request.write(data);
		request.end();
		request.on('response', function(response) {
			var status = response.statusCode;
			var data = null;
			response.on('data', function(chunk) {
				if(data)
					data += chunk;
				else
					data = chunk;
			});
			response.on('end', function() {
				if(app.debug)
					twin.log(method+"<<<"+twin.url.format(url)+"<<< "+data);
				callback(null, status, response.headers, data);
				client.on('error', function(err) {});
				client.destroy();
			});
			response.on('error', function(err) {
				client.destroy();
				callback(err);
			});
		});
	},
	validate: function(response) {
		if(!response || (typeof(response) != "object") || (response.status !== 0))
			throw this.decodeException(response && response.value || response);
		return response.value;
	},
	decodeException: function(response) {
		if(!response || typeof(response) != "object")
			return new Error("Exception: null response");
		var status = (response['status'] || 13);
		var info = response;
		do {
			console.log("HONK");
			info = info.cause || info.value;
			var message = info.message || "No message";
		} while(message.match(/STA thread/) && info.cause);
		var error = new Error();
		error.message = message;
		error.name = info.class || 'Error';
		if(info.stackTrace) {
			var stack = error.name+": "+message;
			var oldstack = error.stack;
			if(oldstack.indexOf('\n'))
				oldstack = oldstack.substring(oldstack.indexOf('\n'));
			info.stackTrace.forEach(function(frame) {
				stack += "\n    at "+frame.className+"."+frame.methodName+" ("+frame.fileName+":"+frame.lineNumber+")";
			});
			error.stack = stack + oldstack;
		}
		return error;
	},
	window: function(callback) {
		this.desktop.waitForChild(null, null, callback);
	},
}

/* Example *

var x = new twin.Application("http://10.250.10.119:4444");
// x.debug = true;
x.desiredCapabilities['applicationName'] = 'notepad';
x.open(function() {
	twin.log("ID: "+x.sessionId);
	twin.log(x.desktop.toString());
	
	var wind;

	twin.seq()
	.seq(function() {
		x.window(this);
	})
	.seq(function(w) {
		wind = w;
		this();
	})
	.par(function() {
		twin.seq().seq(function() {
			wind.descendants(twin.Criteria.controlType('MenuItem'), this);
		}).flatten().parEach(function(child) {
			twin.log(child.toString());
			this();
		}).empty().seq(this).catch(this);
	})
	.par(function() {
		twin.seq()
		.seq(function() {
			wind.screenshot(this);
		}).seq(function(shot) {
			twin.log("Got screenshot, type is: "+shot.contentType+", length is "+shot.data.length);
			this();
		}).empty().seq(this).catch(this);
	})
	.seq(function() { wind.type("hello \u00a7am", this); })
	.seq(function() { wind.descendant(twin.Criteria.controlType('MenuItem').and(twin.Criteria.name('Edit')), this); })
	.seq(function(edit) { edit.click(this); })
	.seq(function() { wind.descendant(twin.Criteria.controlType('MenuItem').and(twin.Criteria.name('Select All')), this); })
	.seq(function(selectAll) { selectAll.click(this); })
	.seq(function() { wind.descendant(twin.Criteria.controlType('MenuItem').and(twin.Criteria.name('Edit')), this); })
	.seq(function(edit) { edit.click(this); })
	.seq(function() { wind.descendant(twin.Criteria.controlType('MenuItem').and(twin.Criteria.name('Copy')), this); })
	.seq(function(copy) { copy.click(this); })
	.seq(function() { x.clipboard.text(this); })
	.seq(function(text) { console.log(text); this(); })
	.catch(function(err) {
		console.error(err.stack || err);
	})
	.seq(function() {
		twin.log("Closing");
		x.close();
	});
});

*/
