using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Xamarin.PropertyEditing.Reflection
{
	public class EnumDependencyPropertyInfo<T>
		: DependencyPropertyInfo, IHavePredefinedValues<T>
	{
		public EnumDependencyPropertyInfo (PropertyInfo propertyInfo, object dependencyProperty)
			: base (propertyInfo, dependencyProperty)
		{
			string[] names = Enum.GetNames (propertyInfo.PropertyType);
			Array values = Enum.GetValues (propertyInfo.PropertyType);

			var predefinedValues = new Dictionary<string, T> (names.Length);
			for (int i = 0; i < names.Length; i++) {
				predefinedValues.Add (names[i], (T)values.GetValue (i));
			}

			PredefinedValues = predefinedValues;

			FlagsAttribute flags = PropertyInfo.PropertyType.GetCustomAttribute<FlagsAttribute> ();
			if (IsValueCombinable = flags != null) {
				DynamicBuilder.RequestOrOperator<T> ();
				DynamicBuilder.RequestHasFlagMethod<T> ();
				DynamicBuilder.RequestCaster<IReadOnlyList<T>> ();
			}
		}

		public bool IsConstrainedToPredefined => true;

		public bool IsValueCombinable
		{
			get;
		}

		public IReadOnlyDictionary<string, T> PredefinedValues
		{
			get;
		}

		public override async Task SetValueAsync<TValue> (object target, ValueInfo<TValue> info)
		{
			TValue value = info.Value;

			IReadOnlyList<T> values = value as IReadOnlyList<T>;
			if (values != null) {
				if (!IsValueCombinable)
					throw new ArgumentException ("Can not set a combined value on a non-combinable type", nameof (value));

				Func<T, T, T> or = DynamicBuilder.GetOrOperator<T> ();

				T realValue = values.Count > 0 ? values[0] : default (T);
				for (int i = 1; i < values.Count; i++) {
					realValue = or (realValue, values[i]);
				}

				SetValueCore (target, new ValueInfo<object> {
					Source = ValueSource.Local,
					Value = realValue
				});
			} else {
				object convertedValue = Enum.ToObject (PropertyInfo.PropertyType, value);

				SetValueCore (target, new ValueInfo<object> {
					Source = ValueSource.Local,
					Value = convertedValue
				});
			}
		}

		public override async Task<ValueInfo<TValue>> GetValueAsync<TValue> (object target)
		{
			ValueInfo<object> info = GetValueCore (target);
			ValueInfo<TValue> finalInfo = new ValueInfo<TValue> {
				ValueDescriptor = info.ValueDescriptor,
				Source = info.Source
			};

			if (typeof (TValue) == typeof (IReadOnlyList<T>)) {
				T realValue = (T)info.Value;

				Func<T, T, bool> hasFlag = DynamicBuilder.GetHasFlagMethod<T> ();

				List<T> values = new List<T> ();
				foreach (T value in PredefinedValues.Values) {
					if (hasFlag (realValue, value))
						values.Add (value);
				}

				Func<object, TValue> caster = DynamicBuilder.GetCaster<TValue> ();
				finalInfo.Value = caster (values);
			} else
				finalInfo.Value = (TValue)info.Value;

			return finalInfo;
		}
	}
}
