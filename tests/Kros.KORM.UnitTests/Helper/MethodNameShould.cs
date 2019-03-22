using FluentAssertions;
using Kros.KORM.Helper;
using Xunit;

namespace Kros.KORM.UnitTests.Helper
{
    public class MethodNameShould
    {
        [Fact]
        public void ReturnFunctionName()
        {
            MethodName<Foo>.GetName(p => p.TestMethod()).Should().Be("TestMethod");
        }

        [Fact]
        public void ReturnFunctionNameWhenFunctionHaveParameter()
        {
            MethodName<Foo>.GetName(p => p.TestMethod("lorem ipsum")).Should().Be("TestMethod");
        }

        public class Foo
        {
            public void TestMethod()
            {
            }

            public void TestMethod(string param)
            {
                var a = param;
            }
        }
    }
}
