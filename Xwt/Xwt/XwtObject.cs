// 
// XwtObject.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Xwt.Engine;
using Xwt.Backends;

namespace Xwt
{
	public abstract class XwtObject: IFrontend
	{
		object backend;

		internal ToolkitEngine ToolkitEngine { get; set; }
		
		protected XwtObject (object backend): this (backend, ToolkitEngine.CurrentEngine)
		{
		}
		
		protected XwtObject (object backend, ToolkitEngine toolkit)
		{
			this.backend = backend;
			ToolkitEngine = ToolkitEngine.CurrentEngine;
		}

		protected XwtObject ()
		{
			ToolkitEngine = ToolkitEngine.CurrentEngine;
		}

		ToolkitEngine IFrontend.ToolkitEngine {
			get { return ToolkitEngine; }
		}

		protected object Backend {
			get {
				LoadBackend ();
				return backend;
			}
			set {
				backend = value;
			}
		}
		
		object IFrontend.Backend {
			get { return Backend; }
		}

		protected void LoadBackend ()
		{
			if (backend == null) {
				backend = OnCreateBackend ();
				if (backend == null)
					throw new InvalidOperationException ("No backend found for widget: " + GetType ());
				OnBackendCreated ();
			}
		}
		
		protected virtual void OnBackendCreated ()
		{
		}
		
		protected virtual object OnCreateBackend ()
		{
			throw new NotImplementedException ();
		}
		
		internal static object GetBackend (XwtObject w)
		{
			return w != null ? w.Backend : null;
		}
	}
}

