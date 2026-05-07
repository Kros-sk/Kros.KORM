using Kros.KORM.Helper;
using Xunit;

namespace Kros.KORM.UnitTests.Helper
{
    public class PropertyNameShould
    {
        [Fact]
        public void ReturnPropertyName()
        {
            Assert.Equal("Prop1", PropertyName<Foo>.GetPropertyName(p => p.Prop1));
        }

        private class Foo
        {
            public int Prop1 { get; set; }
        }
    }
}
