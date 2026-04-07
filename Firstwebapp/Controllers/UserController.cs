using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;
using System.Security.Claims;

namespace Thekdar.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // GET: /User/List â€” Admin only (contractors shouldn't see user list)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> List(string filter = "All")
        {
            List<UserModel> users;

            switch (filter)
            {
                case "Active":
                    users = await _userService.GetActiveUsers();
                    break;
                case "Inactive":
                    users = await _userService.GetInactiveUsers();
                    break;
                default:
                    users = await _userService.GetAllUsers();
                    break;
            }

            ViewBag.CurrentFilter = filter;
            return View(users);
        }

        // GET: /User/Details/{id} â€” Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: /User/Register â€” Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register â€” Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _authService.RegisterEmployee(model);
                    if (result)
                    {
                        TempData["Success"] = $"{model.Name} has been added successfully!";
                        return RedirectToAction("List");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "Unable to register the user right now.");
                    return View(model);
                }

                ModelState.AddModelError("", "Unable to register the user right now.");
            }
            return View(model);
        }

        // GET: /User/Edit/{id} â€” Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null) return NotFound();

            var model = new UserEditViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Age = user.Age,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                HasProfilePicture = user.ProfilePicture != null
            };

            return View(model);
        }

        // POST: /User/Edit/{id} â€” Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.UpdateUser(model);
                    TempData["Success"] = string.IsNullOrWhiteSpace(model.NewPassword)
                        ? $"{model.Name}'s details updated successfully."
                        : $"{model.Name}'s details updated successfully. The login password was reset.";
                    return RedirectToAction("Details", new { id = model.Id });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "Unable to update the user right now.");
                }
            }
            return View(model);
        }

        // POST: /User/Deactivate/{id} â€” Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                await _userService.DeactivateUser(id);
                TempData["Success"] = "Employee deactivated successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to deactivate the user right now.";
            }

            return RedirectToAction("List");
        }

        // POST: /User/Activate/{id} â€” Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                await _userService.ActivateUser(id);
                TempData["Success"] = "Employee activated successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to activate the user right now.";
            }

            return RedirectToAction("List");
        }
        
        // GET: /User/GetProfilePicture/{id}
        public async Task<IActionResult> GetProfilePicture(int id)
        {
            try
            {
                var user = await _userService.GetUserById(id);
                if (user?.ProfilePicture == null)
                    return NotFound();

                // Authorization: Only admins or the user themselves can view profile pictures
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                
                if (!isAdmin && currentUserId != id.ToString())
                    return Forbid();

                return File(user.ProfilePicture, user.ProfilePictureContentType ?? "image/jpeg");
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegistrationSuccess(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null) return NotFound();
            return View(user);
        }
    }
}
