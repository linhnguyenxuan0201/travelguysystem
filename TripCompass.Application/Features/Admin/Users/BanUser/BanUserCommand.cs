using MediatR;

namespace TripCompass.Application.Features.Admin.Users.BanUser
{
    public class BanUserCommand : IRequest<bool>
    {
        public long UserId { get; set; }
        public string Reason { get; set; } = "Violation of terms";
    }
}
