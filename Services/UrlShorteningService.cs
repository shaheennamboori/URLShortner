using Microsoft.EntityFrameworkCore;
using System;
using URLShortner.Models;

namespace URLShortner.Services
{
    internal sealed class UrlShorteningService(ApplicationDbContext dbContext)
    {
        private readonly Random _random = new();
        public async Task<string> GenerateUniqueCode()
        {
            var codeChars = new char[ShortLinkSettings.Length];

            while (true)
            {
                for (var i = 0; i < ShortLinkSettings.Length; i++)
                {
                    var randomIndex = _random.Next(ShortLinkSettings.Alphabet.Length);

                    codeChars[i] = ShortLinkSettings.Alphabet[randomIndex];
                }
                var code = new string(codeChars);

                if (!await dbContext.ShortenedUrls.AnyAsync(s => s.Code == code))
                {
                    return code;
                }
            }
        }
    }
}
