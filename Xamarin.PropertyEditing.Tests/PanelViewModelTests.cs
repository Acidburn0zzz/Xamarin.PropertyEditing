﻿using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Xamarin.PropertyEditing.Reflection;
using Xamarin.PropertyEditing.ViewModels;

namespace Xamarin.PropertyEditing.Tests
{
	[TestFixture]
	public class PanelViewModelTests
	{
		private class TestClass
		{
			public string Property
			{
				get;
				set;
			}
		}

		private class TestClassSub
			: TestClass
		{
			public int SubProperty
			{
				get;
				set;
			}
		}

		[Test]
		public async Task PropertiesAddedFromEditor ()
		{
			var provider = new ReflectionEditorProvider ();
			var obj = new TestClass();
			var editor = await provider.GetObjectEditorAsync (obj);
			Assume.That (editor.Properties.Count, Is.EqualTo (1));

			var vm = new PanelViewModel (provider);
			vm.SelectedObjects.Add (obj);

			Assert.That (vm.Properties, Is.Not.Empty);
			Assert.That (vm.Properties[0].Property, Is.EqualTo (editor.Properties.Single()));
		}

		[Test]
		[Description ("When editors of two different types are selected, the properties that are common should be listed")]
		public void PropertiesFromCommonSubset ()
		{
			var obj1 = new TestClass();
			var obj2 = new TestClassSub();

			var sharedPropertyMock = new Mock<IPropertyInfo> ();
			sharedPropertyMock.SetupGet (pi => pi.Type).Returns (typeof(string));
			var subPropertyMock = new Mock<IPropertyInfo> ();
			subPropertyMock.SetupGet (pi => pi.Type).Returns (typeof(int));

			var editor1Mock = new Mock<IObjectEditor> ();
			editor1Mock.SetupGet (oe => oe.Properties).Returns (new[] { sharedPropertyMock.Object });
			var editor2Mock = new Mock<IObjectEditor> ();
			editor2Mock.SetupGet (oe => oe.Properties).Returns (new[] { sharedPropertyMock.Object, subPropertyMock.Object });

			var providerMock = new Mock<IEditorProvider> ();
			providerMock.Setup (ep => ep.GetObjectEditorAsync (obj1)).ReturnsAsync (editor1Mock.Object);
			providerMock.Setup (ep => ep.GetObjectEditorAsync (obj2)).ReturnsAsync (editor2Mock.Object);

			var vm = new PanelViewModel (providerMock.Object);
			vm.SelectedObjects.Add (obj1);

			Assume.That (vm.Properties.Count, Is.EqualTo (1));
			Assume.That (vm.Properties[0].Property, Is.EqualTo (sharedPropertyMock.Object));

			// Reflection property info equate actually fails on the same property across class/subclass
			vm.SelectedObjects.Add (obj2);
			Assert.That (vm.Properties.Count, Is.EqualTo (1));
			Assert.That (vm.Properties.Single ().Property, Is.EqualTo (sharedPropertyMock.Object));
		}

		[Test]
		[Description ("When editors of two different types are selected, the properties that are common should be listed")]
		public void PropertiesReducesToCommonSubset ()
		{
			var obj1 = new TestClass ();
			var obj2 = new TestClassSub ();

			var sharedPropertyMock = new Mock<IPropertyInfo> ();
			sharedPropertyMock.SetupGet (pi => pi.Type).Returns (typeof (string));
			var subPropertyMock = new Mock<IPropertyInfo> ();
			subPropertyMock.SetupGet (pi => pi.Type).Returns (typeof (int));

			var editor1Mock = new Mock<IObjectEditor> ();
			editor1Mock.SetupGet (oe => oe.Properties).Returns (new[] { sharedPropertyMock.Object });
			var editor2Mock = new Mock<IObjectEditor> ();
			editor2Mock.SetupGet (oe => oe.Properties).Returns (new[] { sharedPropertyMock.Object, subPropertyMock.Object });

			var providerMock = new Mock<IEditorProvider> ();
			providerMock.Setup (ep => ep.GetObjectEditorAsync (obj1)).ReturnsAsync (editor1Mock.Object);
			providerMock.Setup (ep => ep.GetObjectEditorAsync (obj2)).ReturnsAsync (editor2Mock.Object);

			var vm = new PanelViewModel (providerMock.Object);
			vm.SelectedObjects.Add (obj2);

			Assume.That (vm.Properties.Count, Is.EqualTo (2));
			Assume.That (vm.Properties.Select (v => v.Property), Contains.Item (sharedPropertyMock.Object));
			Assume.That (vm.Properties.Select (v => v.Property), Contains.Item (subPropertyMock.Object));

			// Reflection property info equate actually fails on the same property across class/subclass
			vm.SelectedObjects.Add (obj1);
			Assert.That (vm.Properties.Count, Is.EqualTo (1));
			Assert.That (vm.Properties.Select (v => v.Property), Contains.Item (sharedPropertyMock.Object));
		}
	}
}