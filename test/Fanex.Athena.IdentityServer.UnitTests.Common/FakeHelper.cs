namespace Fanex.Athena.IdentityServer.UnitTests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using AutoFixture;
    using AutoFixture.Dsl;

    public static class FakeHelper
    {
        private static readonly Fixture Fixture = new();

        public static T Create<T>(Expression<Func<T, object>> propertyPicker, object value)
        {
            return Fixture.Build<T>().With(propertyPicker, value).Create();
        }

        public static T Create<T>(IEnumerable<FakeProperty<T>> properties)
        {
            return Fixture.Build<T>().WithPropertiesPicker(properties).Create();
        }

        public static T Create<T>()
        {
            return Fixture.Create<T>();
        }

        public static IEnumerable<T> CreateMany<T>(int count = 3)
        {
            return Fixture.CreateMany<T>(count);
        }

        public static IEnumerable<T> CreateMany<T>(Expression<Func<T, object>> propertyPicker, object value,
                                                   int count = 3)
        {
            return Fixture.Build<T>().With(propertyPicker, value).CreateMany(count);
        }

        public static IEnumerable<T> CreateMany<T>(IEnumerable<FakeProperty<T>> properties, int count = 3)
        {
            return Fixture.Build<T>().WithPropertiesPicker(properties).CreateMany(count);
        }

        public static ICustomizationComposer<T> WithPropertiesPicker<T>(this ICustomizationComposer<T> fixture,
                                                                        IEnumerable<FakeProperty<T>> properties)
        {
            foreach (var property in properties)
            {
                fixture.With(property.PropertyPicker, property.Value);
            }

            return fixture;
        }
    }

    public class FakeProperty<T>
    {
        public Expression<Func<T, object>> PropertyPicker;
        public object Value;

        public FakeProperty(Expression<Func<T, object>> propertyPicker, object value)
        {
            PropertyPicker = propertyPicker;
            Value = value;
        }
    }
}
