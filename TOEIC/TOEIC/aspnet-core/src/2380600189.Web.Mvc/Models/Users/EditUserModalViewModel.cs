using TOEIC.Roles.Dto;
using TOEIC.Users.Dto;
using System.Collections.Generic;
using System.Linq;

namespace TOEIC.Web.Models.Users;

public class EditUserModalViewModel
{
    public UserDto User { get; set; }

    public IReadOnlyList<RoleDto> Roles { get; set; }

    public bool UserIsInRole(RoleDto role)
    {
        return User.RoleNames != null && User.RoleNames.Any(r => r == role.NormalizedName);
    }
}
