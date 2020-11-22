using System;
using System.Security.Principal;

namespace AspMvcLibrary.AspMvcLib
{
  public interface IBasePrincipal : IPrincipal
  {
    Guid SessionId { get; }
    bool IsLogin { get; }
    bool IsVerified { get; }
    long AccountId { get; }
    string Email { get; }
    string Role { get; }
  }
}
