using IMS.Authorization;
using IMS.Common_Interfaces;
using IMS.Enums;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    [CustomAuthorize(privilege: EnumPrivilegesName.USER_PERMISSIONS)]
    public class UserPermissionsController : Controller
    {
        private readonly IUserPermissionsService _userPermissionsService;

        public UserPermissionsController(IUserPermissionsService userPermissionsService)
        {
            _userPermissionsService = userPermissionsService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "User Permissions";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var list = await _userPermissionsService.GetUsersForPermissionsAsync();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetFeatureHierarchy()
        {
            var tree = await _userPermissionsService.GetFeatureHierarchyAsync();
            return Json(tree);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserPermissions(long userId)
        {
            if (userId <= 0) return Json(new List<UserPermissionItemDto>());
            var list = await _userPermissionsService.GetUserPermissionsAsync(userId);
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> ReplicatePermissions([FromBody] ReplicatePermissionsRequest request)
        {
            if (request?.SourceUserId <= 0 || request?.TargetUserId <= 0)
                return BadRequest("Invalid source or target user.");
            await _userPermissionsService.ReplicatePermissionsAsync(request.SourceUserId, request.TargetUserId);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions([FromBody] SavePermissionsRequest request)
        {
            if (request?.UserId <= 0)
                return BadRequest("Invalid user.");
            await _userPermissionsService.SaveUserPermissionsAsync(request.UserId, request.Permissions ?? new List<UserPermissionItemDto>());
            return Ok();
        }
    }

    public class ReplicatePermissionsRequest
    {
        public long SourceUserId { get; set; }
        public long TargetUserId { get; set; }
    }

    public class SavePermissionsRequest
    {
        public long UserId { get; set; }
        public List<UserPermissionItemDto>? Permissions { get; set; }
    }
}
