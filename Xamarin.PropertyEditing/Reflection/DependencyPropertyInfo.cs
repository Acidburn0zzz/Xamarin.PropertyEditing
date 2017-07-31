using System;
using System.Linq;
using System.Reflection;

namespace Xamarin.PropertyEditing.Reflection
{
	public class DependencyPropertyInfo
		: ReflectionPropertyInfo
	{
		public DependencyPropertyInfo (PropertyInfo propertyInfo, object dependencyProperty)
			: base (propertyInfo)
		{
			this.dependencyProperty = dependencyProperty;
			Setup ();
		}

		public override ValueSources ValueSources => ValueSources.All;

		protected override ValueInfo<object> GetValueCore (object target)
		{
			object value = DependencyObject_GetValue.Invoke (target, new [] { this.dependencyProperty });
			ValueSource source = GetValueSource (target);

			return new ValueInfo<object> {
				Value = value,
				Source = source
			};
		}

		protected override void SetValueCore (object target, ValueInfo<object> realValue)
		{
			if (realValue.Source == ValueSource.Default) {
				DependencyObject_ClearValue.Invoke (target, new[] { this.dependencyProperty });
				return;
			}

			base.SetValueCore (target, realValue);
		}

		private readonly object dependencyProperty;

		private ValueSource GetValueSource (object target)
		{
			object source = GetDependencyValueSource.Invoke (null, new[] { target, this.dependencyProperty });
			object baseValueSource = ValueSource_GetBaseValueSource.Invoke (source, null);
			WpfValueSource wpfSource = (WpfValueSource) baseValueSource;

			switch (wpfSource) {
				case WpfValueSource.Default:
					return ValueSource.Default;
				case WpfValueSource.DefaultStyle:
				case WpfValueSource.DefaultStyleTrigger:
					return ValueSource.DefaultStyle;
				case WpfValueSource.Style:
				case WpfValueSource.StyleTrigger:
				case WpfValueSource.ImplicitStyleReference:
					return ValueSource.Style;
				case WpfValueSource.Inherited:
					return ValueSource.Inherited;
				default:
				case WpfValueSource.Local:
				case WpfValueSource.Unknown:
					return ValueSource.Local;
			}
		}

		private static MethodInfo DependencyObject_GetValue, DependencyObject_SetValue;
		private static MethodInfo DependencyObject_ClearValue;
		private static MethodInfo GetDependencyValueSource;

		private static MethodInfo ValueSource_GetBaseValueSource;

		private enum WpfValueSource
		{
			Unknown,
			Default,
			Inherited,
			DefaultStyle,
			DefaultStyleTrigger,
			Style,
			TemplateTrigger,
			StyleTrigger,
			ImplicitStyleReference,
			ParentTemplate,
			ParentTemplateTrigger,
			Local
		}

		private static void Setup ()
		{
			if (DependencyObject_GetValue != null)
				return;

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies ();
			Assembly windowsBase = assemblies.Single (a => a.GetName ().Name == "WindowsBase");
			Type dpType = windowsBase.GetType ("System.Windows.DependencyProperty");
			Type doType = windowsBase.GetType ("System.Windows.DependencyObject");

			DependencyObject_GetValue = doType.GetMethod ("GetValue");
			DependencyObject_SetValue = doType.GetMethod ("SetValue", new[] { dpType, typeof (object) });
			DependencyObject_ClearValue = doType.GetMethod ("ClearValue", new[] { dpType });

			Assembly presentationFramework = assemblies.Single (a => a.GetName ().Name == "PresentationFramework");
			GetDependencyValueSource = presentationFramework.GetType ("System.Windows.DependencyPropertyHelper").GetMethod ("GetValueSource");

			Type vsType = presentationFramework.GetType ("System.Windows.ValueSource");
			ValueSource_GetBaseValueSource = vsType.GetProperty ("BaseValueSource").GetMethod;
		}
	}
}
