﻿using EDennis.AspNet.Base;
using EDennis.AspNetIdentityServer.Data;
using EDennis.AspNetIdentityServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace Hr.UserApi.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase {

        private readonly UserManager<AspNetIdentityUser> _userManager;

        public UserController(UserManager<AspNetIdentityUser> userManager) {
            _userManager = userManager;

        }

        public static Regex idPattern = new Regex("[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}");
        public static Regex lpPattern = new Regex("(?:\\(\\s*loginProvider\\s*=\\s*'?)([^,']+)(?:'?\\s*,\\s*providerKey\\s*=\\s*'?)([^)']+)(?:'?\\s*\\))");


        [HttpGet("{pathParameter:alpha}")]
        public async Task<IActionResult> GetAsync([FromRoute] string pathParameter) {
            var user = await FindAsync(pathParameter);
            if (user == null)
                return NotFound();
            else {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentClaims = await _userManager.GetClaimsAsync(user);
                var userEditModel = new UserEditModel {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = currentRoles,
                    Claims = currentClaims
                };

                return Ok(userEditModel);

            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] UserEditModel userEditModel) {

            userEditModel.Id ??= Guid.NewGuid().ToString();
            userEditModel.UserName ??= userEditModel.Email;

            var user = new AspNetIdentityUser {
                Id = userEditModel.Id,
                Email = userEditModel.Email,
                NormalizedEmail = userEditModel.Email.ToUpper(),
                UserName = userEditModel.UserName,
                NormalizedUserName = userEditModel.UserName.ToUpper()
            };
            var hashed = _userManager.PasswordHasher.HashPassword(user, userEditModel.Password);
            user.PasswordHash = hashed;

            var results = new List<IdentityResult> {
                await _userManager.CreateAsync(user)
            };

            if (userEditModel.Roles != null && userEditModel.Roles.Count() > 0)
                results.Add(await _userManager.AddToRolesAsync(user, userEditModel.Roles));

            if (userEditModel.Claims != null && userEditModel.Claims.Count() > 0)
                results.Add(await _userManager.AddClaimsAsync(user, userEditModel.Claims));

            var failures = results.SelectMany(r => r.Errors);

            if (failures.Count() > 0)
                return Conflict(failures);
            else
                return Ok(userEditModel);

        }


        [HttpPatch("{pathParameter:alpha}")]
        public async Task<IActionResult> PatchAsync([FromBody] JsonElement jsonElement, [FromRoute] string pathParameter) {

            var user = await FindAsync(pathParameter);
            if (user == null)
                return NotFound();

            bool securityUpdated = false;

            if (jsonElement.TryGetString("Email", out string email)) {
                user.Email = email;
                user.NormalizedEmail = email.ToUpper();
            }

            if (jsonElement.TryGetString("PhoneNumber", out string phoneNumber)) {
                user.PhoneNumber = phoneNumber;
            }

            if (jsonElement.TryGetString("UserName", out string userName)) {
                user.UserName = userName;
                user.NormalizedUserName = userName.ToUpper();
                securityUpdated = true;
            }

            if (jsonElement.TryGetString("Password", out string password)) {
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, password);
                securityUpdated = true;
            }

            if (jsonElement.TryGetBoolean("EmailConfirmed", out bool emailConfirmed))
                user.EmailConfirmed = emailConfirmed;
            if (jsonElement.TryGetBoolean("PhoneNumberConfirmed", out bool phoneNumberConfirmed))
                user.PhoneNumberConfirmed = phoneNumberConfirmed;

            if (jsonElement.TryGetBoolean("TwoFactorEnabled", out bool twoFactorEnabled))
                user.TwoFactorEnabled = twoFactorEnabled;
            if (jsonElement.TryGetBoolean("LockoutEnabled", out bool lockoutEnabled))
                user.LockoutEnabled = lockoutEnabled;
            if (jsonElement.TryGetDateTimeOffset("LockoutEnd", out DateTimeOffset? lockoutEnd))
                user.LockoutEnd = lockoutEnd;
            if (jsonElement.TryGetInt32("AccessFailedCount", out int accessFailedCount))
                user.AccessFailedCount = accessFailedCount;



            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            if (securityUpdated)
                user.SecurityStamp = Guid.NewGuid().ToString();

            var results = new List<IdentityResult> {
                await _userManager.UpdateAsync(user)
            };


            var hasRoles = jsonElement.TryGetProperty("Roles", out JsonElement _);
            var hasClaims = jsonElement.TryGetProperty("Claims", out JsonElement _);

            var userEditModel = JsonSerializer.Deserialize<UserEditModel>(jsonElement.GetRawText());

            results.AddRange(await UpdateRolesAndClaimsAsync(user, userEditModel, hasRoles, hasClaims));

            var failures = results.SelectMany(r=>r.Errors);

            if (failures.Count() > 0)
                return BadRequest(failures);
            else
                return NoContent();
        }


        [HttpDelete("{pathParameter:alpha}")]
        public async Task<IActionResult> Delete([FromRoute] string pathParameter) {
            var user = await FindAsync(pathParameter);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Conflict(result.Errors);
            else
                return NoContent();
        }


        private async Task<IEnumerable<IdentityResult>> UpdateRolesAndClaimsAsync(AspNetIdentityUser user,
            UserEditModel userEditModel, bool hasRoles, bool hasClaims) {

            List<IdentityResult> results = new List<IdentityResult>();


            if (hasRoles) {
                var currentRoles = await _userManager.GetRolesAsync(user);
                results.Add(await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(userEditModel.Roles)));
                results.Add(await _userManager.AddToRolesAsync(user, userEditModel.Roles.Except(currentRoles)));
            }

            if (hasClaims) {
                var currentClaims = await _userManager.GetClaimsAsync(user);
                results.Add(await _userManager.RemoveClaimsAsync(user, currentClaims.Except(userEditModel.Claims)));
                results.Add(await _userManager.AddClaimsAsync(user, userEditModel.Claims.Except(currentClaims)));
            }

            return results;

        }


        private async Task<AspNetIdentityUser> FindAsync(string pathParameter) {
            if (pathParameter.Contains("@"))
                return await _userManager.FindByEmailAsync(pathParameter);
            else if (idPattern.IsMatch(pathParameter))
                return await _userManager.FindByIdAsync(pathParameter);
            else
                return await _userManager.FindByNameAsync(pathParameter);
        }

    }
}