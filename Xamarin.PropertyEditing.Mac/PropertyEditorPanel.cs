﻿using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Foundation;
using AppKit;
using Xamarin.PropertyEditing;
using System.Collections.ObjectModel;
using Xamarin.PropertyEditing.ViewModels;
using System.Diagnostics.Contracts;
using Xamarin.PropertyEditing.Reflection;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.PropertyEditing.Mac
{
	public partial class PropertyEditorPanel : AppKit.NSView
	{
		NSSearchField propertyFilter;
		NSComboBox propertyArrangeMode;

		public PropertyEditorPanel ()
		{
			Initialize ();
		}

		// Called when created from unmanaged code
		public PropertyEditorPanel (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public PropertyEditorPanel (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// when this property changes, need to create new datasource
		IEditorProvider editorProvider;
		NSOutlineView propertyTable;
		PropertyTableDataSource dataSource;
		PanelViewModel viewModel;
		INotifyCollectionChanged PropertiesChanged => viewModel?.Properties as INotifyCollectionChanged;

		public IEditorProvider EditorProvider {
			get { return editorProvider; }
			set {
				// if the propertiesChanged is already subscribed to, remove the event
				if (PropertiesChanged != null)
					PropertiesChanged.CollectionChanged -= HandleCollectionChanged;

				// Populate the Properety Table
				editorProvider = value;
				viewModel = new PanelViewModel (editorProvider);
				dataSource = new PropertyTableDataSource (viewModel);
				propertyTable.Delegate = new PropertyTableDelegate (dataSource);
				propertyTable.DataSource = dataSource;

				// if propertiesChanged exists
				if (PropertiesChanged != null)
					PropertiesChanged.CollectionChanged += HandleCollectionChanged;
			}
		}

		void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			propertyTable.ReloadData ();
		}


		public ICollection<object> SelectedItems => this.dataSource.SelectedItems;

		// Shared initialization code
		void Initialize ()
		{
			AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;

			propertyFilter = new NSSearchField (new CGRect (10, Frame.Height - 25, 170, 24));
			propertyFilter.PlaceholderString = "Property Filter";
			propertyFilter.ControlSize = NSControlSize.Regular;
			AddSubview (propertyFilter);

			var label = new NSTextField (new CGRect (250, Frame.Height - 28, 150, 24)) {
				BackgroundColor = NSColor.Clear,
				TextColor = NSColor.Black,
				Editable = false,
				Bezeled = false,
				ControlSize = NSControlSize.Regular,
				StringValue = "Arrange By:"
			};
			AddSubview (label);

			propertyArrangeMode = new NSComboBox (new CGRect (320, Frame.Height - 25, 100, 24)) {
				Editable = false,
				ControlSize = NSControlSize.Regular,
			};
			AddSubview (propertyFilter);


			var enumValues = Enum.GetValues (typeof (PropertyArrangeMode));

			foreach (var item in enumValues) {
				propertyArrangeMode.Add (new NSString (item.ToString ()));
			}
			propertyArrangeMode.SelectItem (0);

			AddSubview (propertyArrangeMode);

			// If either the Filter Mode or PropertySearchFilter Change Filter the Data
			propertyArrangeMode.SelectionChanged += PropertyFilterMode_Changed;
			propertyFilter.Changed += PropertyFilterMode_Changed;

			// create a table view and a scroll view
			var tableContainer = new NSScrollView (new CGRect (10, Frame.Height - 240, Frame.Width - 20, Frame.Height - 30)) {
				AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable
			};

			propertyTable = new FirstResponderOutlineView () {
				AutoresizingMask = NSViewResizingMask.WidthSizable,
				RowHeight = 24,
			};

			// create columns for the panel
			NSTableColumn propertiesList = new NSTableColumn ("PropertiesList") { Title = "Properties" };
			NSTableColumn propertyEditors = new NSTableColumn ("PropertyEditors") { Title = "Editors" };
			propertiesList.Width = 200;
			propertyEditors.Width = 255;
			propertyTable.AddColumn (propertiesList);
			propertyTable.AddColumn (propertyEditors);

			// Set OutlineTableColumn or the arrows showing children/expansion will not be drawn
			propertyTable.OutlineTableColumn = propertiesList;

			// add the panel to the window
			tableContainer.DocumentView = propertyTable;
			AddSubview (tableContainer);
		}

		void PropertyFilterMode_Changed (object sender, EventArgs e)
		{
			PropertyArrangeMode filterMode;
			Enum.TryParse<PropertyArrangeMode> (propertyArrangeMode.GetItemObject (propertyArrangeMode.SelectedIndex).ToString (), out filterMode);
			FilterData (propertyFilter.Cell.Title, filterMode);
		}

		public void FilterData (string title, PropertyArrangeMode arrangeMode)
		{
			viewModel.FilterData (title, arrangeMode);
		}

		class FirstResponderOutlineView : NSOutlineView
		{
			[Export ("validateProposedFirstResponder:forEvent:")]
			public bool validateProposedFirstResponder (NSResponder responder, NSEvent ev)
			{
				return true;
			}
		}
	}
}
