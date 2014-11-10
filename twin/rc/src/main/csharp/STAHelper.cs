// [Twin] Copyright eBay Inc., Twin authors, and other contributors.
// This file is provided to you under the terms of the Apache License, Version 2.0.
// See LICENSE.txt and NOTICE.txt for license and copyright information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

using Twin.Logging;

namespace Twin
{
	delegate void Action();
	delegate object Func();

	internal class Job {
		internal Exception exception;
		internal bool done;
		internal Func func;
		internal object result;
		public Job(Func func) {
			this.func = func;
		}
		internal void Run() {
			try {
				result = func.Invoke();
			} catch (Exception e) {
				exception = e;
			} finally {
				done = true;
			}
		}
		internal object evaluate() {
			if(!done)
				throw new Exception("Should be done");
			if(exception != null) {
				// we need to throw the exception.
				// we want the java behaviour where the stack is preserved unless you overwrite it,
				// however "throw exception" will overwrite the stack.
				// one solution is throw new TargetInvocationException(exception)
				// however this breaks any associated catch clauses
				// a pretty nice compromise is to throw a new exception of the same type, 
				// with the original chained to it.
				string message = "Error on STA thread, exception is chained.";
				Type type = exception.GetType();
				
				ConstructorInfo stringExceptionConstructor = type.GetConstructor(new Type[]{typeof(string), typeof(Exception)});
				if(stringExceptionConstructor != null) {
					Exception ex = (Exception)stringExceptionConstructor.Invoke(new object[]{message, exception});
					throw ex;
				}
				ConstructorInfo exceptionConstructor = type.GetConstructor(new Type[]{typeof(Exception)});
				if(exceptionConstructor != null) {
					Exception ex = (Exception)exceptionConstructor.Invoke(new object[]{exception});
					throw ex;
				}
				// in this case, we couldn't find a suitable constructor, so we rethrow the exception.
				throw exception;
			}
			return result;
		}
	}
	
	internal class STAHelper {
		public static object Invoke(Func func) {
			Job j = new Job(func);
			if(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
				j.Run();
				return j.evaluate();
			} else lock(j) {
				STAHelper helper = STAHelper.Instance;
				helper.Enqueue(j);
				Logger.Current.Trace("Job created on STA thread, waiting");
				Monitor.Wait(j);
				Logger.Current.Trace("Job finished on STA thread, returning");
				return j.evaluate();				
			}
		}
		public static void Invoke(Action func) {
			Invoke(delegate() { func.Invoke(); return null; });
		}
		
		private static STAHelper instance;
		private static STAHelper Instance {
			[MethodImpl(MethodImplOptions.Synchronized)]
			get {
				if(instance == null)
					instance = new STAHelper();
				return instance;
			}
		}
		
		private Queue<Job> jobs = new Queue<Job>();
		
		private void Enqueue(Job j) {
			lock(jobs) {
				jobs.Enqueue(j);
				Monitor.PulseAll(jobs);
			}
		}
		
		private STAHelper() {
			Thread thread = new Thread(this.Run);
			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			Logger.Current.Info("STA thread for COM access started");
		}
		
		private void Run() {
			while(true) {
				Job current = null;
				lock(jobs) {
					if(jobs.Count == 0) {
						Monitor.Wait(jobs);
						continue;
					} else {
						current = jobs.Dequeue();
					}
				}
				current.Run();
				lock(current) {
					Monitor.PulseAll(current);
				}
			}
		}
	}
}
