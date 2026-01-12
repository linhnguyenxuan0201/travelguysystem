using MediatR;

namespace TripCompass.Application.Features.Admin.Users.UnbanUser
{
    public class UnbanUserCommand : IRequest<bool>
    {
        public long UserId { get; set; }
    }
}
