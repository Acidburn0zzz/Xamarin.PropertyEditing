﻿using System;
using AppKit;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	public class PropertyTableDelegate : NSTableViewDelegate
	{
		PropertyTableDataSource DataSource;

		public PropertyTableDelegate (PropertyTableDataSource datasource)
		{
			this.DataSource = datasource;
		}

		// the table is looking for this method, picks it up automagically
		public override NSView GetViewForItem (NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			PropertyViewModel property = DataSource.ViewModel.Properties [(int)row];
			string cellIdentifier;

			// This pattern allows you reuse existing views when they are no-longer in use.
			// If the returned view is null, you instance up a new view
			// If a non-null view is returned, you modify it enough to reflect the new data


			// TODO: can do this differently
			// Setup view based on the column selected
			NSView view = new NSView ();
			switch (tableColumn.Title) {
			case "Properties":
				//view.StringValue = DataSource.Properties [(int)row].Name;
				cellIdentifier = "cell";
				view = (NSTextView)tableView.MakeView (cellIdentifier, this);
				if (view == null) {
					view = new NSTextView ();
					view.Identifier = cellIdentifier;
				}
				((NSTextView)view).Value = property.Property.Name;
				//((StringEditorControl)view).StringEditor.StringValue = property.Property.Name;

				break;
			case "Editors":
				//view.StringValue = DataSource.Properties [(int)row].PropertyValue;
				cellIdentifier = property.GetType ().Name;
				view = tableView.MakeView (cellIdentifier, this);
				if (view == null) {
					if (property is StringPropertyViewModel) {
						view = new StringEditorControl ();
						view.Identifier = cellIdentifier;
					}
				}
				if (property is StringPropertyViewModel)
					((StringEditorControl)view).ViewModel = (StringPropertyViewModel)property;

				break;
			}

			return view;
		}
	}
}
