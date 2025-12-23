using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Application.Auth
{
    public interface ICurrentUserService
    {
        long UserId { get; }
        string? Email { get; }
    }
}
