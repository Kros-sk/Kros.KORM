using FluentAssertions;
using Kros.KORM.Helper;
using Xunit;

namespace Kros.KORM.UnitTests.Helper
{
    public class PropertyNameShould
    {
        [Fact]
        public void ReturnPropertyName()
        {
            PropertyName<Foo>.GetPropertyName(p => p.Prop1).Should().Be("Prop1");
        }

        private class Foo
        {
            public int Prop1 { get; set; }
        }
    }
}
