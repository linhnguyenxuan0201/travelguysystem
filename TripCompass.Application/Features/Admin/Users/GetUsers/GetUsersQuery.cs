using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Users.GetUsers
{
    public class GetUsersQuery : IRequest<(List<UserListItemDto> Items, int TotalCount)>
    {
        public string? SearchTerm { get; set; }
        public bool? IsBanned { get; set; } // Filter by status
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
