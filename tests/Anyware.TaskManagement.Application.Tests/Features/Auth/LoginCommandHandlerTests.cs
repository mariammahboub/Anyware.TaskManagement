using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Auth.Commands.Login;
using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Enums;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Tests.Features.Auth
{
    public sealed class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<IJwtService> _jwtService = new();
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ExpiryHours"] = "1"
            })
            .Build();

        private LoginCommandHandler CreateHandler() => new(
            _userRepo.Object,
            _passwordHasher.Object,
            _jwtService.Object,
            _configuration);

        private static User MakeUser(string email = "user@test.com", string hash = "valid_hash")
            => User.Create("Test User", email, hash);

        public LoginCommandHandlerTests()
        {
            _jwtService
                .Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
                .Returns("mock_access_token");

            _jwtService
                .Setup(j => j.GenerateRefreshToken())
                .Returns("mock_refresh_token");
        }
        [Fact]
        public async Task Handle_ValidCredentials_ReturnsAuthResponseWithTokens()
        {
            var user = MakeUser();
            _userRepo
                .Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasher
                .Setup(h => h.Verify("correct_password", "valid_hash"))
                .Returns(true);

            var handler = CreateHandler();
            var result = await handler.Handle(
                new LoginCommand("user@test.com", "correct_password"),
                CancellationToken.None);
            result.AccessToken.Should().Be("mock_access_token");
            result.RefreshToken.Should().Be("mock_refresh_token");
            result.UserId.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.Role.Should().Be(UserRole.User.ToString());
            result.AccessTokenExpiry.Should().BeCloseTo(
                DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Handle_ValidCredentials_CallsGenerateAccessTokenWithUser()
        {
            var user = MakeUser();
            _userRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            await CreateHandler().Handle(
                new LoginCommand("user@test.com", "password"),
                CancellationToken.None);

            _jwtService.Verify(j => j.GenerateAccessToken(user), Times.Once);
        }
        [Fact]
        public async Task Handle_EmailNotFound_ThrowsUnauthorizedException()
        {
            _userRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var act = async () => await CreateHandler().Handle(
                new LoginCommand("unknown@test.com", "any_password"),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("*Invalid email or password*");
        }
        [Fact]
        public async Task Handle_WrongPassword_ThrowsUnauthorizedException()
        {
            _userRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeUser());
            _passwordHasher
                .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            var act = async () => await CreateHandler().Handle(
                new LoginCommand("user@test.com", "wrong_password"),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>()
                .WithMessage("*Invalid email or password*");
        }
        [Fact]
        public async Task Handle_InvalidCredentials_ErrorMessageDoesNotRevealWhichFieldIsWrong()
        {
            _userRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            UnauthorizedException? notFoundEx = null;
            try
            {
                await CreateHandler().Handle(
                    new LoginCommand("x@x.com", "password"),
                    CancellationToken.None);
            }
            catch (UnauthorizedException ex) { notFoundEx = ex; }

            _userRepo
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeUser());
            _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            UnauthorizedException? wrongPasswordEx = null;
            try
            {
                await CreateHandler().Handle(
                    new LoginCommand("x@x.com", "wrong"),
                    CancellationToken.None);
            }
            catch (UnauthorizedException ex) { wrongPasswordEx = ex; }

            notFoundEx!.Message.Should().Be(wrongPasswordEx!.Message);
        }
    }
}