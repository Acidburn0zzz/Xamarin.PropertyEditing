﻿using System;

using AppKit;
using Foundation;
using Xamarin.PropertyEditing.Reflection;

namespace Xamarin.PropertyEditing.Mac.Standalone
{
	public partial class ViewController : NSViewController
	{
		public ViewController (IntPtr handle) : base (handle)
		{
			
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Do any additional setup after loading the view.

			PropertyPanel.EditorProvider = new ReflectionEditorProvider ();
		}

		public override NSObject RepresentedObject {
			get {
				return base.RepresentedObject;
			}
			set {
				base.RepresentedObject = value;
				// Update the view, if already loaded.
			}
		}

		// load panel from active designer item, clear it if none selected
		partial void OnClickEvent (NSObject sender)
		{
			if (PropertyPanel.SelectedItems.Contains (sender)) {
				PropertyPanel.SelectedItems.Remove (sender);
			} else {
				PropertyPanel.SelectedItems.Add (sender);
			}
		}
	}
}
