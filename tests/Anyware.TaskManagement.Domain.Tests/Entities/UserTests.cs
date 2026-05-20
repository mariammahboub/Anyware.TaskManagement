using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Enums;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Domain.Tests.Entities
{
    public sealed class UserTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsUserWithCorrectProperties()
        {
            var user = User.Create("John Doe", "john@example.com", "hashed_pw");
            user.Name.Should().Be("John Doe");
            user.Email.Should().Be("john@example.com");
            user.PasswordHash.Should().Be("hashed_pw");
            user.Role.Should().Be(UserRole.User);
            user.IsDeleted.Should().BeFalse();
            user.Id.Should().NotBe(Guid.Empty);
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            user.UpdatedAt.Should().BeNull();
            user.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void Create_WithAdminRole_SetsRoleToAdmin()
        {
            var user = User.Create("Admin", "admin@test.com", "hash", UserRole.Admin);
            user.Role.Should().Be(UserRole.Admin);
        }

        [Theory]
        [InlineData("JOHN@EXAMPLE.COM", "john@example.com")]
        [InlineData("  Admin@Anyware.COM  ", "admin@anyware.com")]
        [InlineData("Test@Test.Com", "test@test.com")]
        public void Create_EmailIsNormalizedToLowercaseAndTrimmed(
            string inputEmail, string expectedEmail)
        {
            var user = User.Create("Name", inputEmail, "hash");
            user.Email.Should().Be(expectedEmail);
        }

        [Fact]
        public void Create_NameIsTrimmed()
        {
            var user = User.Create("  John Doe  ", "email@test.com", "hash");
            user.Name.Should().Be("John Doe");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankName_ThrowsArgumentException(string name)
        {
            var act = () => User.Create(name, "email@test.com", "hash");
            act.Should().Throw<ArgumentException>()
                .WithParameterName("name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankEmail_ThrowsArgumentException(string email)
        {
            var act = () => User.Create("Name", email, "hash");
            act.Should().Throw<ArgumentException>()
                .WithParameterName("email");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankPasswordHash_ThrowsArgumentException(string hash)
        {
            var act = () => User.Create("Name", "email@test.com", hash);
            act.Should().Throw<ArgumentException>()
                .WithParameterName("passwordHash");
        }


        [Fact]
        public void SoftDelete_SetsIsDeletedToTrue()
        {
            var user = User.Create("Name", "email@test.com", "hash");
            user.SoftDelete();
            user.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public void SoftDelete_SetsUpdatedAt()
        {
            var user = User.Create("Name", "email@test.com", "hash");
            user.SoftDelete();
            user.UpdatedAt.Should().NotBeNull();
            user.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }


        [Fact]
        public void Restore_AfterSoftDelete_SetsIsDeletedToFalse()
        {
            var user = User.Create("Name", "email@test.com", "hash");
            user.SoftDelete();
            user.Restore();
            user.IsDeleted.Should().BeFalse();
        }
        [Fact]
        public void UpdateName_WithValidName_UpdatesNameAndSetsUpdatedAt()
        {
            var user = User.Create("Old Name", "email@test.com", "hash");

            user.UpdateName("New Name");

            user.Name.Should().Be("New Name");
            user.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateName_WithBlankName_ThrowsArgumentException(string name)
        {
            var user = User.Create("Name", "email@test.com", "hash");
            var act = () => user.UpdateName(name);
            act.Should().Throw<ArgumentException>();
        }
        [Fact]
        public void UpdatePasswordHash_WithValidHash_UpdatesHash()
        {
            var user = User.Create("Name", "email@test.com", "old_hash");
            user.UpdatePasswordHash("new_hash");
            user.PasswordHash.Should().Be("new_hash");
            user.UpdatedAt.Should().NotBeNull();
        }
    }
}