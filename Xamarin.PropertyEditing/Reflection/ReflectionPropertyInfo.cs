using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Xamarin.PropertyEditing.Reflection
{
	public class ReflectionPropertyInfo
		: IPropertyInfo, IEquatable<ReflectionPropertyInfo>
	{
		public ReflectionPropertyInfo (PropertyInfo propertyInfo)
		{
			this.propertyInfo = propertyInfo;

			this.category = new Lazy<string> (() => {
				CategoryAttribute categoryAttribute = this.propertyInfo.GetCustomAttribute<CategoryAttribute> ();
				return categoryAttribute?.Category;
			});

			this.typeConverter = new Lazy<List<TypeConverter>> (() => {
				List<TypeConverter> converters = new List<TypeConverter> ();

				var attributes = this.propertyInfo.GetCustomAttributes<TypeConverterAttribute> ().Concat (this.propertyInfo.PropertyType.GetCustomAttributes<TypeConverterAttribute> ());
				foreach (TypeConverterAttribute attribute in attributes) {
					Type type = System.Type.GetType (attribute.ConverterTypeName);
					if (type == null)
						continue;

					converters.Add ((TypeConverter)Activator.CreateInstance (type));
				}

				return converters;
			});
		}

		public string Name => this.propertyInfo.Name;

		public Type Type => this.propertyInfo.PropertyType;

		public string Category => this.category.Value;

		public bool CanWrite => this.propertyInfo.CanWrite;

		public virtual ValueSources ValueSources => ValueSources.Local;

		public IReadOnlyList<PropertyVariation> Variations => EmtpyVariations;

		public IReadOnlyList<IAvailabilityConstraint> AvailabilityConstraints => EmptyConstraints;

		public virtual async Task SetValueAsync<T> (object target, ValueInfo<T> valueInfo)
		{
			object realValue = valueInfo.Value;
			object converted;
			if (TryConvertFromValue (valueInfo.Value, out converted)) {
				realValue = converted;
			} else if (realValue != null && !this.propertyInfo.PropertyType.IsInstanceOfType (valueInfo.Value)) {
				realValue = Convert.ChangeType (valueInfo.Value, this.propertyInfo.PropertyType);
			}

			SetValueCore (target, new ValueInfo<object> {
				Source = valueInfo.Source,
				Value = realValue,
				ValueDescriptor = valueInfo.ValueDescriptor
			});
		}

		public virtual async Task<ValueInfo<T>> GetValueAsync<T> (object target)
		{
			ValueInfo<object> valueInfo = GetValueCore (target);
			object value = valueInfo.Value;
			T converted;
			if (TryConvertToValue (value, out converted)) {
				value = converted;
			} else if (value != null && !(value is T)) {
				if (typeof(T) == typeof(string))
					value = value.ToString ();
				else
					value = Convert.ChangeType (value, typeof(T));
			}

			return new ValueInfo<T> {
				Value = (T)value,
				Source = valueInfo.Source,
				ValueDescriptor = valueInfo.ValueDescriptor
			};
		}

		public bool Equals (ReflectionPropertyInfo other)
		{
			if (ReferenceEquals (null, other))
				return false;
			if (ReferenceEquals (this, other))
				return true;

			return this.propertyInfo.Equals (other.propertyInfo);
		}

		public override bool Equals (object obj)
		{
			if (ReferenceEquals (null, obj))
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != this.GetType ())
				return false;

			return Equals ((ReflectionPropertyInfo)obj);
		}

		public override int GetHashCode ()
		{
			return this.propertyInfo.GetHashCode ();
		}

		public static bool operator == (ReflectionPropertyInfo left, ReflectionPropertyInfo right)
		{
			return Equals (left, right);
		}

		public static bool operator != (ReflectionPropertyInfo left, ReflectionPropertyInfo right)
		{
			return !Equals (left, right);
		}

		protected PropertyInfo PropertyInfo => this.propertyInfo;

		protected virtual ValueInfo<object> GetValueCore (object target)
		{
			return new ValueInfo<object> {
				Value = this.propertyInfo.GetValue (target),
				Source = ValueSource.Local
			};
		}

		protected virtual void SetValueCore (object target, ValueInfo<object> realValue)
		{
			this.propertyInfo.SetValue (target, realValue.Value);
		}

		private readonly Lazy<List<TypeConverter>> typeConverter;
		private readonly Lazy<string> category;

		private readonly PropertyInfo propertyInfo;

		private static readonly IAvailabilityConstraint[] EmptyConstraints = new IAvailabilityConstraint[0];
		private static readonly PropertyVariation[] EmtpyVariations = new PropertyVariation[0];

		private bool TryConvertToValue<T> (object value, out T converted)
		{
			converted = default(T);
			List<TypeConverter> converters = this.typeConverter.Value;

			for (int i = 0; i < converters.Count; i++) {
				TypeConverter c = converters[i];
				if (c.CanConvertTo (typeof(T))) {
					converted = (T) c.ConvertTo (value, typeof(T));
					return true;
				}
			}

			return false;
		}

		private bool TryConvertFromValue<T> (T value, out object converted)
		{
			converted = null;
			List<TypeConverter> converters = this.typeConverter.Value;
			for (int i = 0; i < converters.Count; i++) {
				TypeConverter c = converters[i];
				if (c.CanConvertFrom (typeof(T))) {
					converted = c.ConvertFrom (value);
					return true;
				}
			}

			return false;
		}
	}
}