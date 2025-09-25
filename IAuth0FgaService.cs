using FGA_POC.Dtos;

namespace FGA_POC;

public interface IAuth0FgaService
{
    // === Account relations ===
    Task AddAccountToUser(Account account, User user);
    Task RemoveAccountFromUser(Account account, User user);
    Task AddUserTo(Account account, User user, Role role);
    Task RemoveUserFrom(Account account, User user, Role role);
    Task AddUserTo(Account account, User user, Role role, TimeSpan duration);

    // === Workspace relations ===
    Task AddWorkspaceToAccount(Workspace workspace, Account account);
    Task RemoveWorkspaceFromAccount(Workspace workspace, Account account);
    Task AddUserTo(Workspace workspace, User user, Role role);
    Task RemoveUserFrom(Workspace workspace, User user, Role role);
    Task AddUserTo(Workspace workspace, User user, Role role, TimeSpan duration);
    // === Policy relations ===
    Task AddPolicyToWorkspace(Policy policy, Workspace workspace);
    Task RemovePolicyFromWorkspace(Policy policy, Workspace workspace);
    Task AddUserTo(Policy policy, User user, Role role);
    Task RemoveUserFrom(Policy policy, User user, Role role);
    Task AddUserTo(Policy policy, User user, Role role, TimeSpan duration);

    // === Configuration relations ===
    Task AddConfigurationToWorkspace(Configuration config, Workspace workspace);
    Task RemoveConfigurationFromWorkspace(Configuration config, Workspace workspace);
    Task AddUserTo(Configuration config, User user, Role role);
    Task RemoveUserFrom(Configuration config, User user, Role role);
    Task AddUserTo(Configuration config, User user, Role role, TimeSpan duration);

    // === Access checks ===
    Task<bool?> CheckAccess(User user, Role role, object obj);

}
