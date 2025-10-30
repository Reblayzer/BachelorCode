using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Services;

public sealed class UserService : IUserService
{
  private readonly UserManager<ApplicationUser> _users;
  private readonly SignInManager<ApplicationUser> _signIn;

  public UserService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
  {
    _users = users;
    _signIn = signIn;
  }

  public async Task<(bool Succeeded, IEnumerable<string> Errors, string? UserId)> CreateAsync(string email, string password)
  {
    var user = new ApplicationUser { UserName = email, Email = email };
    var res = await _users.CreateAsync(user, password);
    return (res.Succeeded, res.Errors.Select(e => e.Description), user.Id);
  }

  public async Task<string?> GenerateEmailConfirmationTokenAsync(string userId)
  {
    var user = await _users.FindByIdAsync(userId);
    if (user is null) return null;
    return await _users.GenerateEmailConfirmationTokenAsync(user);
  }

  public async Task<(string? UserId, string? Email)> FindByIdAsync(string userId)
  {
    var u = await _users.FindByIdAsync(userId);
    return (u?.Id, u?.Email);
  }

  public async Task<(string? UserId, string? Email)> FindByEmailAsync(string email)
  {
    var u = await _users.FindByEmailAsync(email);
    return (u?.Id, u?.Email);
  }

  public async Task<bool> ConfirmEmailAsync(string userId, string token)
  {
    var u = await _users.FindByIdAsync(userId);
    if (u is null) return false;
    var r = await _users.ConfirmEmailAsync(u, token);
    return r.Succeeded;
  }

  public async Task<bool> IsEmailConfirmedAsync(string userId)
  {
    var u = await _users.FindByIdAsync(userId);
    if (u is null) return false;
    return await _users.IsEmailConfirmedAsync(u);
  }

  public async Task<bool> PasswordSignInAsync(string email, string password, bool isPersistent = true, bool lockoutOnFailure = true)
  {
    var user = await _users.FindByEmailAsync(email);
    if (user is null) return false;
    var res = await _signIn.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    return res.Succeeded;
  }

  public Task SignOutAsync() => _signIn.SignOutAsync();
}
