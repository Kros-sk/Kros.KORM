using FluentAssertions;
using Kros.KORM.Helper;
using System;
using Xunit;

namespace Kros.KORM.UnitTests.Helper
{
    public class TypeExtensionsShould
    {
        [Theory]
        [InlineData(typeof(FooWithImplicitDefaultCtor), true, true)]
        [InlineData(typeof(FooWithExplicitDefaultCtor), true, true)]
        [InlineData(typeof(FooWithPrivateDefaultCtor), true, true)]
        [InlineData(typeof(FooWithOneDefaultCtorAndOneAnother), true, true)]
        [InlineData(typeof(FooWithoutDefaultCtor), true, false)]
        [InlineData(typeof(BarRecord), true, false)]
        [InlineData(typeof(BarRecordWithMoreCtors), false, false)]
        public void ShouldGetConstructor(Type type, bool hasCtor, bool isDefault)
        {
            (System.Reflection.ConstructorInfo ctor, bool isDefault) info = type.GetConstructor();
            info.isDefault.Should().Be(isDefault);
            if (hasCtor)
            {
                info.ctor.Should().NotBeNull();
            }
            else
            {
                info.ctor.Should().BeNull();
            }

            if (hasCtor && info.isDefault)
            {
                info.ctor.GetParameters().Should().BeEmpty();
            }
        }

        public class FooWithImplicitDefaultCtor
        {
        }

        public class FooWithExplicitDefaultCtor
        {
            public FooWithExplicitDefaultCtor()
            {
            }
        }

        public class FooWithPrivateDefaultCtor
        {
            private FooWithPrivateDefaultCtor()
            {
            }
        }

        public class FooWithOneDefaultCtorAndOneAnother
        {
            public FooWithOneDefaultCtorAndOneAnother()
            {
            }
#pragma warning disable IDE0060 // Remove unused parameter
            public FooWithOneDefaultCtorAndOneAnother(int i)
#pragma warning restore IDE0060 // Remove unused parameter
            {
            }
        }

        public class FooWithoutDefaultCtor
        {
#pragma warning disable IDE0060 // Remove unused parameter
            public FooWithoutDefaultCtor(int i)
#pragma warning restore IDE0060 // Remove unused parameter
            {
            }
        }

        public record BarRecord(int Id);

        public record BarRecordWithMoreCtors
        {
            public string FirstName { get; init; }
            public string LastName { get; init; }

            public BarRecordWithMoreCtors(string firstName)
                => (FirstName, LastName) = (firstName, string.Empty);

            public BarRecordWithMoreCtors(string firstName, string lastName)
                => (FirstName, LastName) = (firstName, lastName);
        }
    }
}
