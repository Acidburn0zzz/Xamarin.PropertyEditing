﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using AppKit;
using CoreGraphics;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class RectangleEditorControl : PropertyEditorControl
	{
		public RectangleEditorControl ()
		{
			var xLabel = new NSTextView (new CGRect (0, 0, 20, 20)) {
				Value = "X:"
			};
			XEditor = new NSTextField (new CGRect (25, 0, 50, 20));
			XEditor.BackgroundColor = NSColor.Clear;
			XEditor.StringValue = string.Empty;
			XEditor.Activated += (sender, e) => {
				ViewModel.Value = new Rectangle (XEditor.IntValue, YEditor.IntValue, WidthEditor.IntValue, HeightEditor.IntValue);
			};

			var yLabel = new NSTextView (new CGRect (75, 0, 20, 20)) {
				Value = "Y:"
			};
			YEditor = new NSTextField (new CGRect (80, 0, 50, 20));
			YEditor.BackgroundColor = NSColor.Clear;
			YEditor.StringValue = string.Empty;
			YEditor.Activated += (sender, e) => {
				ViewModel.Value = new Rectangle (XEditor.IntValue, YEditor.IntValue, WidthEditor.IntValue, HeightEditor.IntValue);
			};

			var widthLabel = new NSTextView (new CGRect (0, 30, 40, 20)) {
				Value = "Width:"
			};
			WidthEditor = new NSTextField (new CGRect (45, 30, 50, 20));
			WidthEditor.BackgroundColor = NSColor.Clear;
			WidthEditor.StringValue = string.Empty;
			WidthEditor.Activated += (sender, e) => {
				ViewModel.Value = new Rectangle (XEditor.IntValue, YEditor.IntValue, WidthEditor.IntValue, HeightEditor.IntValue);
			};

			var heightLabel = new NSTextView (new CGRect (100, 30, 40, 20)) {
				Value = "Height:"
			};
			HeightEditor = new NSTextField (new CGRect (145, 30, 50, 20));
			HeightEditor.BackgroundColor = NSColor.Clear;
			HeightEditor.StringValue = string.Empty;
			HeightEditor.Activated += (sender, e) => {
				ViewModel.Value = new Rectangle (XEditor.IntValue, YEditor.IntValue, WidthEditor.IntValue, HeightEditor.IntValue);
			};

			// update the value on 'enter'
			XEditor.Activated += (sender, e) => {
				ViewModel.Value = new Rectangle (XEditor.IntValue, XEditor.IntValue, WidthEditor.IntValue, HeightEditor.IntValue);
			};

			AddSubview (xLabel);
			AddSubview (XEditor);
			AddSubview (yLabel);
			AddSubview (YEditor);
			AddSubview (widthLabel);
			AddSubview (WidthEditor);
			AddSubview (heightLabel);
			AddSubview (HeightEditor);
		}

		internal NSTextField XEditor { get; set; }
		internal NSTextField YEditor { get; set; }
		internal NSTextField WidthEditor { get; set; }
		internal NSTextField HeightEditor { get; set; }

		internal new RectanglePropertyViewModel ViewModel {
			get { return (RectanglePropertyViewModel)base.ViewModel; }
			set { base.ViewModel = value; }
		}

		protected override void HandlePropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (RectanglePropertyViewModel.Value)) {
				UpdateModelValue ();
			}
		}

		protected override void UpdateModelValue ()
		{
			base.UpdateModelValue ();
			XEditor.IntValue = ViewModel.Value.X;
			YEditor.IntValue = ViewModel.Value.Y;
			WidthEditor.IntValue = ViewModel.Value.Width;
			HeightEditor.IntValue = ViewModel.Value.Height;
		}

		protected override void HandleErrorsChanged (object sender, System.ComponentModel.DataErrorsChangedEventArgs e)
		{
			UpdateErrorsDisplayed (ViewModel.GetErrors (ViewModel.Property.Name));
		}

		protected override void UpdateErrorsDisplayed (IEnumerable errors)
		{
			if (ViewModel.HasErrors) {
				XEditor.BackgroundColor = NSColor.Red;
				YEditor.BackgroundColor = NSColor.Red;
				WidthEditor.BackgroundColor = NSColor.Red;
				HeightEditor.BackgroundColor = NSColor.Red;
				Debug.WriteLine ("Your input triggered an error:");
				foreach (var error in errors) {
					Debug.WriteLine (error.ToString () + "\n");
				}
			} else {
				XEditor.BackgroundColor = NSColor.Clear;
				YEditor.BackgroundColor = NSColor.Clear;
				WidthEditor.BackgroundColor = NSColor.Clear;
				HeightEditor.BackgroundColor = NSColor.Clear;
				SetEnabled ();
			}
		}

		protected override void SetEnabled ()
		{
			XEditor.Editable = ViewModel.Property.CanWrite;
			YEditor.Editable = ViewModel.Property.CanWrite;
			WidthEditor.Editable = ViewModel.Property.CanWrite;
			HeightEditor.Editable = ViewModel.Property.CanWrite;
		}
	}
}
