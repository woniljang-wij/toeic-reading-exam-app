using Abp.Authorization;
using TOEIC.Authorization.Roles;
using TOEIC.Authorization.Users;

namespace TOEIC.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
