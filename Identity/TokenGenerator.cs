﻿using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity;

public class TokenGenerator
{
    public string GenerateToken(string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = "PleaseKeepThisKeySafelyAndSecurelyThisWillComeInHandy"u8.ToArray();
        var claims = new List<Claim> { 
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
            new(JwtRegisteredClaimNames.Email, email), 
            new(JwtRegisteredClaimNames.Sub, email)};
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            Issuer = "Identity.localhost",
            Audience = "localhost",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(
                key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
