using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

namespace Xamarin.PropertyEditing.Reflection
{
	public class ReflectionObjectEditor
		: IObjectEditor
	{
		public ReflectionObjectEditor (object target)
		{
			if (target == null)
				throw new ArgumentNullException (nameof (target));

			this.target = target;

			Type targetType = target.GetType ();
			this.isWpfType = GetIsWpfType (targetType);

			IReadOnlyDictionary<string, object> dependencyProperties = (this.isWpfType) ? GetDependencyProperties (target) : null;

			foreach (PropertyInfo property in targetType.GetProperties ()) {
				DebuggerBrowsableAttribute browsable = property.GetCustomAttribute<DebuggerBrowsableAttribute> ();
				if (browsable != null && browsable.State == DebuggerBrowsableState.Never) {
					continue;
				}

				if (CheckAvailability (property)) {
					ReflectionPropertyInfo propInfo;

					object dp;
					if (dependencyProperties != null && dependencyProperties.TryGetValue (property.Name, out dp)) {
						if (property.PropertyType.IsEnum)
							propInfo = (ReflectionPropertyInfo)Activator.CreateInstance (typeof (EnumDependencyPropertyInfo<>).MakeGenericType (Enum.GetUnderlyingType (property.PropertyType)), property, dp);
						else
							propInfo = new DependencyPropertyInfo (property, dp);
					} else if (property.PropertyType.IsEnum) {
						propInfo = (ReflectionPropertyInfo)Activator.CreateInstance (typeof (ReflectionEnumPropertyInfo<>).MakeGenericType (Enum.GetUnderlyingType (property.PropertyType)), property);
					} else
						propInfo = new ReflectionPropertyInfo (property);

					this.properties.Add (propInfo);
				}
			}
		}

		public event EventHandler<EditorPropertyChangedEventArgs> PropertyChanged;

		public object Target => this.target;

		public IReadOnlyCollection<IPropertyInfo> Properties => this.properties;

		public IObjectEditor Parent => null;

		public IReadOnlyList<IObjectEditor> DirectChildren => EmptyDirectChildren;

		public async Task SetValueAsync<T> (IPropertyInfo property, ValueInfo<T> value, PropertyVariation variation = null)
		{
			if (property == null)
				throw new ArgumentNullException (nameof (property));
			
			ReflectionPropertyInfo info = property as ReflectionPropertyInfo;
			if (info == null)
				throw new ArgumentException();

			await info.SetValueAsync (this.target, value);
			OnPropertyChanged (info);
		}

		public Task<ValueInfo<T>> GetValueAsync<T> (IPropertyInfo property, PropertyVariation variation = null)
		{
			if (property == null)
				throw new ArgumentNullException (nameof (property));
			
			ReflectionPropertyInfo info = property as ReflectionPropertyInfo;
			if (info == null)
				throw new ArgumentException();

			return info.GetValueAsync<T> (this.target);
		}

		private readonly object target;
		private readonly bool isWpfType;
		private readonly List<ReflectionPropertyInfo> properties = new List<ReflectionPropertyInfo> ();

		private static Version OSVersion;

		private static readonly IObjectEditor[] EmptyDirectChildren = new IObjectEditor[0];

		private static Type DependencyObjectType;
		private static MethodInfo GetDependencyPropertyName;

		protected virtual void OnPropertyChanged (IPropertyInfo property)
		{
			PropertyChanged?.Invoke (this, new EditorPropertyChangedEventArgs (property));
		}

		private static bool GetIsWpfType (Type type)
		{
			Type baseType = type;
			while (baseType != null) {
				if (baseType.Name == "DependencyObject") {
					DependencyObjectType = baseType;
					return true;
				}

				baseType = baseType.BaseType;
			}

			return false;
		}

		private static IReadOnlyDictionary<string, object> GetDependencyProperties (object target)
		{
			Dictionary<string, object> properties = new Dictionary<string, object> ();

			Type type = target.GetType ();
			foreach (FieldInfo field in type.GetFields (BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public)) {
				if (field.FieldType.Name != "DependencyProperty")
					continue;

				if (GetDependencyPropertyName == null) {
					GetDependencyPropertyName = field.FieldType.GetProperty ("Name").GetMethod;
				}

				object dproperty = field.GetValue (target);
				string name = (string)GetDependencyPropertyName.Invoke (dproperty, null);
				properties.Add (name, dproperty);
			}

			return properties;
		}

		private static bool CheckAvailability (PropertyInfo property)
		{
			Attribute availibility = property.GetCustomAttributes ().FirstOrDefault (a => a.GetType ().Name == "IntroducedAttribute");
			if (availibility == null)
				return true;

			var versionProperty = availibility.GetType ().GetProperty ("Version");
			if (versionProperty == null)
				return false;

			if (OSVersion == null) {
				Type processInfoType = Type.GetType ("Foundation.NSProcessInfo, Xamarin.Mac");
				object processInfo = Activator.CreateInstance (processInfoType);
				object version = processInfoType.GetProperty ("OperatingSystemVersion").GetValue (processInfo);

				Type nsosversionType = version.GetType ();
				int major = (int)Convert.ChangeType (nsosversionType.GetField ("Major").GetValue (version), typeof(int));
				int minor = (int)Convert.ChangeType (nsosversionType.GetField ("Minor").GetValue (version), typeof(int));
				int build = (int)Convert.ChangeType (nsosversionType.GetField ("PatchVersion").GetValue (version), typeof(int));

				OSVersion = new Version (major, minor, build);
				processInfoType.GetMethod ("Dispose").Invoke (processInfo, null);
			}

			Version available = (Version)versionProperty.GetValue (availibility);
			return (OSVersion >= available);
		}
	}
}