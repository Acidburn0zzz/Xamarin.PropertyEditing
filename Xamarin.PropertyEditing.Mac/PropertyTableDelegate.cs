﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AppKit;
using Foundation;
using Xamarin.PropertyEditing.Drawing;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Mac
{
	internal class PropertyTableDelegate
		: NSOutlineViewDelegate
	{
		public PropertyTableDelegate (PropertyTableDataSource datasource)
		{
			this.dataSource = datasource;
		}

		public void UpdateExpansions (NSOutlineView outlineView)
		{
			this.isExpanding = true;

			if (!String.IsNullOrWhiteSpace (this.dataSource.DataContext.FilterText)) {
				outlineView.ExpandItem (null, true);
			} else {
				foreach (IGrouping<string, PropertyViewModel> g in this.dataSource.DataContext.ArrangedProperties) {
					NSObject item;
					if (!this.dataSource.TryGetFacade (g, out item))
						continue;

					if (this.dataSource.DataContext.GetIsExpanded (g.Key))
						outlineView.ExpandItem (item);
					else
						outlineView.CollapseItem (item);
				}
			}
			this.isExpanding = false;
		}

		// the table is looking for this method, picks it up automagically
		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			var facade = (NSObjectFacade)item;
			var vm = facade.Target as PropertyViewModel;
			var group = facade.Target as IGroupingList<string, PropertyViewModel>;
			string cellIdentifier = (group == null) ? vm.GetType ().Name : group.Key;

			// Setup view based on the column
			switch (tableColumn.Identifier) {
				case PropertyEditorPanel.PropertyListColId:
					var view = (UnfocusableTextField)outlineView.MakeView ("label", this);
					if (view == null) {
						view = new UnfocusableTextField {
							Identifier = "label",
							Alignment = NSTextAlignment.Right,
						};
					}

					view.StringValue = ((group == null) ? vm.Property.Name : group.Key) ?? String.Empty;
					return view;

				case PropertyEditorPanel.PropertyEditorColId:
					if (vm == null)
						return null;

					var editor = (PropertyEditorControl)outlineView.MakeView (cellIdentifier + "edits", this);
					if (editor == null) {
						editor = GetEditor (vm, outlineView);
					}

					// we must reset these every time, as the view may have been reused
					editor.ViewModel = vm;
					editor.TableRow = outlineView.RowForItem (item);
					return editor;
			}

			throw new Exception ("Unknown column identifier: " + tableColumn.Identifier);
		}

		PropertyEditorControl GetEditor (PropertyViewModel vm, NSOutlineView outlineView)
		{
			Type[] genericArgs = null;
			Type controlType;
			Type propertyType = vm.GetType ();
			if (!ViewModelTypes.TryGetValue (propertyType, out controlType)) {
				if (propertyType.IsConstructedGenericType) {
					genericArgs = propertyType.GetGenericArguments ();
					propertyType = propertyType.GetGenericTypeDefinition ();
					ViewModelTypes.TryGetValue (propertyType, out controlType);
				}
			}

			if (controlType == null)
				return null;

			if (controlType.IsGenericTypeDefinition) {
				controlType = controlType.MakeGenericType (genericArgs);
			}

			return SetUpEditor (controlType, vm, outlineView);
		}

		public override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
		{
			return (!(item is NSObjectFacade) || !(((NSObjectFacade)item).Target is IGroupingList<string, PropertyViewModel>));
		}

		public override void ItemDidExpand (NSNotification notification)
		{
			if (this.isExpanding)
				return;

			NSObjectFacade facade = notification.UserInfo.Values[0] as NSObjectFacade;
			var group = facade.Target as IGroupingList<string, PropertyViewModel>;
			if (group != null)
				this.dataSource.DataContext.SetIsExpanded (group.Key, isExpanded: true);
		}

		public override void ItemDidCollapse (NSNotification notification)
		{
			if (this.isExpanding)
				return;

			NSObjectFacade facade = notification.UserInfo.Values[0] as NSObjectFacade;
			var group = facade.Target as IGroupingList<string, PropertyViewModel>;
			if (group != null)
				this.dataSource.DataContext.SetIsExpanded (group.Key, isExpanded: false);
		}

		public override nfloat GetRowHeight (NSOutlineView outlineView, NSObject item)
		{
			var facade = (NSObjectFacade)item;
			var group = facade.Target as IGroupingList<string, PropertyViewModel>;
			if (group != null) {
				return 30;
			}

			var vm = (PropertyViewModel)facade.Target;
			var editor = (PropertyEditorControl)outlineView.MakeView (vm.GetType ().Name + "edits", this);
			if (editor == null) {
				editor = GetEditor (vm, outlineView);
			}
			return editor.RowHeight;
		}

		private PropertyTableDataSource dataSource;
		private bool isExpanding;

		// set up the editor based on the type of view model
		private PropertyEditorControl SetUpEditor (Type controlType, PropertyViewModel property, NSOutlineView outline)
		{
			var view = (PropertyEditorControl)Activator.CreateInstance (controlType);
			view.Identifier = property.GetType ().Name;
			view.TableView = outline;

			return view;
		}

		private static readonly Dictionary<Type, Type> ViewModelTypes = new Dictionary<Type, Type> {
			{typeof (StringPropertyViewModel), typeof (StringEditorControl)},
			{typeof (IntegerPropertyViewModel), typeof (IntegerNumericEditorControl)},
			{typeof (FloatingPropertyViewModel), typeof (DecimalNumericEditorControl)},
			{typeof (PropertyViewModel<bool>), typeof (BooleanEditorControl)},
			{typeof (PropertyViewModel<CoreGraphics.CGPoint>), typeof (CGPointEditorControl)},
			{typeof (PropertyViewModel<CoreGraphics.CGRect>), typeof (CGRectEditorControl)},
			{typeof (PredefinedValuesViewModel<>), typeof(PredefinedValuesEditor<>)},
			{typeof (PropertyViewModel<CoreGraphics.CGSize>), typeof (CGSizeEditorControl)},
			{typeof (PropertyViewModel<Point>), typeof (SystemPointEditorControl)},
			{typeof (PropertyViewModel<CommonPoint>), typeof (CommonPointEditorControl) },
			{typeof (PropertyViewModel<Size>), typeof (SystemSizeEditorControl)},
			{typeof (PropertyViewModel<CommonSize>), typeof (CommonSizeEditorControl) },
			{typeof (PropertyViewModel<Rectangle>), typeof (RectangleEditorControl)}
		};
	}
}
