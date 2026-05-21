using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Auth.DTOs;
using Anyware.TaskManagement.Domain.Entities;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Anyware.TaskManagement.Application.Features.Auth.Commands.Register
{
    internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<AuthResponse> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var emailTaken = await _userRepository.ExistsByEmailAsync(
                request.Email, cancellationToken);

            if (emailTaken)
                throw new ConflictException(
                    $"An account with email '{request.Email}' already exists.");

            var passwordHash = _passwordHasher.Hash(request.Password);
            var user = User.Create(request.Name, request.Email, passwordHash);
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours");
            var refreshDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays");
            user.SetRefreshToken(refreshToken, refreshDays);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiry: DateTime.UtcNow.AddHours(expiryHours),
                UserId: user.Id,
                Name: user.Name,
                Email: user.Email,
                Role: user.Role.ToString());
        }
    }
}