using FluentAssertions;
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
            configurator.GetValue("Value").Should().Be("lorem");
        }

        [Fact]
        public void ThrowExceptionIfPropertyIsNotConfigured()
        {
            var configurator = new InjectionConfiguration<Foo>();

            var foo = new Foo() { Id = 1 };
            Action action = () => configurator.GetValue("Value");

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void HaveConfiguredProperty()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty(p => p.Value, () => "lorem");

            configurator.IsInjectable("Value").Should().BeTrue();
        }

        [Fact]
        public void NotHaveConfiguredProperty()
        {
            var configurator = new InjectionConfiguration<Foo>();

            configurator.FillProperty(p => p.Value, () => "lorem");

            configurator.IsInjectable("Id").Should().BeFalse();
        }

        private class Foo
        {
            public int Id { get; set; }

            public string Value { get; set; }
        }
    }
}
