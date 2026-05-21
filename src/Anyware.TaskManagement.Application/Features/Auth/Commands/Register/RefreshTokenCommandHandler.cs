using Anyware.TaskManagement.Application.Common.Exceptions;
using Anyware.TaskManagement.Application.Common.Interfaces;
using Anyware.TaskManagement.Application.Features.Auth.DTOs;
using Anyware.TaskManagement.Domain.Interfaces;
using Anyware.TaskManagement.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Anyware.TaskManagement.Application.Features.Auth.Commands.Register;

internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork     _unitOfWork;
    private readonly IJwtService     _jwtService;
    private readonly IConfiguration  _configuration;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork     unitOfWork,
        IJwtService     jwtService,
        IConfiguration  configuration)
    {
        _userRepository = userRepository;
        _unitOfWork     = unitOfWork;
        _jwtService     = jwtService;
        _configuration  = configuration;
    }

    public async Task<AuthResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken   cancellationToken)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch
        {
            throw new UnauthorizedException("Invalid access token.");
        }

 
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid token claims.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.IsRefreshTokenValid(request.RefreshToken))
            throw new UnauthorizedException(
                "Refresh token is invalid or has expired. Please log in again.");

        var newAccessToken  = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var expiryHours     = _configuration.GetValue<int>("Jwt:ExpiryHours");
        var refreshDays     = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays");

        user.SetRefreshToken(newRefreshToken, refreshDays);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            AccessToken:       newAccessToken,
            RefreshToken:      newRefreshToken,
            AccessTokenExpiry: DateTime.UtcNow.AddHours(expiryHours),
            UserId:            user.Id,
            Name:              user.Name,
            Email:             user.Email,
            Role:              user.Role.ToString());
    }
}
