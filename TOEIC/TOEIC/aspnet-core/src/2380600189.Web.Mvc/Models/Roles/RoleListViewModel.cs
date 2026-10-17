using TOEIC.Roles.Dto;
using System.Collections.Generic;

namespace TOEIC.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}
