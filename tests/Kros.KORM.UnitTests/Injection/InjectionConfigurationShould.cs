using Kros.KORM.Injection;
using System;
using Xunit;

namespace Kros.KORM.UnitTests.Injection
{
    public class InjectionConfigurationShould
    {
        [Fact]
        public void ReturnConfiguredValue()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty(p => p.Value, () => "lorem");

            var foo = new Foo() { Id = 1 };
            Assert.Equal("lorem", configurator.GetValue("Value"));
        }

        [Fact]
        public void ReturnConfiguredValueWhenPropertyNameIsUsed()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty("Value", () => "lorem");

            var foo = new Foo() { Id = 1 };
            Assert.Equal("lorem", configurator.GetValue("Value"));
        }

        [Fact]
        public void ThrowExceptionIfPropertyIsNotConfigured()
        {
            var configurator = new InjectionConfiguration<Foo>();

            var foo = new Foo() { Id = 1 };
            Action action = () => configurator.GetValue("Value");

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void HaveConfiguredProperty()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty(p => p.Value, () => "lorem");

            Assert.True(configurator.IsInjectable("Value"));
        }

        [Fact]
        public void HaveConfiguredPropertyWhenPropertyNameIsUsed()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty("Value", () => "lorem");

            Assert.True(configurator.IsInjectable("Value"));
        }

        [Fact]
        public void NotHaveConfiguredProperty()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty(p => p.Value, () => "lorem");

            Assert.False(configurator.IsInjectable("Id"));
        }

        private class Foo
        {
            public int Id { get; set; }

            public string Value { get; set; }
        }
    }
}
