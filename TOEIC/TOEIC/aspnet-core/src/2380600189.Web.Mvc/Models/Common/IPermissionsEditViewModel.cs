using TOEIC.Roles.Dto;
using System.Collections.Generic;

namespace TOEIC.Web.Models.Common;

public interface IPermissionsEditViewModel
{
    List<FlatPermissionDto> Permissions { get; set; }
}