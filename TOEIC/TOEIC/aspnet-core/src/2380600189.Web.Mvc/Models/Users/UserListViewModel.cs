using TOEIC.Roles.Dto;
using System.Collections.Generic;

namespace TOEIC.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}
