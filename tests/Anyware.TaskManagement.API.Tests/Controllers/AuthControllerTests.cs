using Anyware.TaskManagement.API.Controller;
using Anyware.TaskManagement.Application.Features.Auth.Commands.Login;
using Anyware.TaskManagement.Application.Features.Auth.Commands.Register;
using Anyware.TaskManagement.Application.Features.Auth.DTOs;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.API.Tests.Controllers
{
    public sealed class AuthControllerTests
    {
        private readonly Mock<ISender> _sender = new();
        private readonly AuthController _controller;

        private static readonly AuthResponse SampleAuthResponse = new(
            AccessToken: "access_token_value",
            RefreshToken: "refresh_token_value",
            AccessTokenExpiry: DateTime.UtcNow.AddHours(1),
            UserId: Guid.NewGuid(),
            Name: "Test User",
            Email: "test@test.com",
            Role: "User");

        public AuthControllerTests()
        {
            _controller = new AuthController(_sender.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }
        [Fact]
        public async Task Register_ValidRequest_Returns201WithAuthResponse()
        {
            _sender
                .Setup(s => s.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleAuthResponse);

            var result = await _controller.Register(
                new RegisterRequest("Test User", "test@test.com", "Pass@123"),
                CancellationToken.None);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
            statusResult.Value.Should().BeEquivalentTo(SampleAuthResponse);
        }

        [Fact]
        public async Task Register_DispatchesRegisterCommandWithCorrectValues()
        {
            _sender
                .Setup(s => s.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleAuthResponse);

            await _controller.Register(
                new RegisterRequest("John", "john@test.com", "Pass@123!"),
                CancellationToken.None);

            _sender.Verify(s =>
                s.Send(
                    It.Is<RegisterCommand>(c =>
                        c.Name == "John" &&
                        c.Email == "john@test.com" &&
                        c.Password == "Pass@123!"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        [Fact]
        public async Task Login_ValidCredentials_Returns200WithAuthResponse()
        {
            _sender
                .Setup(s => s.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleAuthResponse);

            var result = await _controller.Login(
                new LoginRequest("test@test.com", "Pass@123"),
                CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            okResult.Value.Should().BeEquivalentTo(SampleAuthResponse);
        }

        [Fact]
        public async Task Login_DispatchesLoginCommandWithCorrectValues()
        {
            _sender
                .Setup(s => s.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SampleAuthResponse);

            await _controller.Login(
                new LoginRequest("admin@anyware.com", "Admin@123"),
                CancellationToken.None);

            _sender.Verify(s =>
                s.Send(
                    It.Is<LoginCommand>(c =>
                        c.Email == "admin@anyware.com" &&
                        c.Password == "Admin@123"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}