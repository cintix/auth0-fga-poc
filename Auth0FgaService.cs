using FGA_POC.Dtos;
using OpenFga.Sdk.Model;
using User = FGA_POC.Dtos.User;

namespace FGA_POC;

using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;

public class Auth0FgaService(ClientConfiguration configuration) : IAuth0FgaService
{
    private readonly OpenFgaClient _fgaClient = new(configuration);

    private string RoleToRelation(Role role)
    {
        return role switch
        {
            Role.Admin => "admin",
            Role.Editor => "editor",
            Role.Reader => "reader",
            Role.Reviewer => "reviewer",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private static string ToFgaDuration(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1) return $"{(int)timeSpan.TotalDays}d";
        if (timeSpan.TotalHours >= 1) return $"{(int)timeSpan.TotalHours}h";
        if (timeSpan.TotalMinutes >= 1) return $"{(int)timeSpan.TotalMinutes}m";
        return $"{(int)timeSpan.TotalSeconds}s";
    }
    
    private async Task WriteTuple(string user, string relation, string obj, TimeSpan duration = default)
    {
        var now = DateTime.UtcNow;
        ClientTupleKey tuple; 
        if (duration != TimeSpan.Zero)
        {
            tuple = new ClientTupleKey
            {
                User = user,
                Relation = relation,
                Object = obj,
                Condition = new RelationshipCondition()
                {
                    Name = "temporary_user_grant",
                    Context = new Dictionary<string, object>
                    {
                        { "grant_time", now },
                        { "grant_duration", ToFgaDuration(duration) }
                    }
                }
            };
        }
        else
        {
            tuple = new ClientTupleKey
            {
                User = user,
                Relation = relation,
                Object = obj
            };
        }

        var writeRequest = new ClientWriteRequest { Writes = [tuple] };
        await _fgaClient.Write(writeRequest);
    }

    private async Task DeleteTuple(string user, string relation, string obj)
    {
        var tuple = new ClientTupleKeyWithoutCondition
        {
            User = user,
            Relation = relation,
            Object = obj
        };

        var deleteRequest = new ClientWriteRequest { Deletes = [tuple] };
        await _fgaClient.Write(deleteRequest);
    }

    // === Account relations ===

    public Task AddAccountToUser(Account account, User user) =>
        WriteTuple($"User:{user.Id}", "admin", $"Account:{account.Id}");

    public Task RemoveAccountFromUser(Account account, User user) =>
        DeleteTuple($"User:{user.Id}", "admin", $"Account:{account.Id}");

    public Task AddUserTo(Account account, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Account:{account.Id}");

    public Task RemoveUserFrom(Account account, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"Account:{account.Id}");

    public Task AddUserTo(Account account, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Account:{account.Id}", duration);

    // === Workspace relations ===
    public Task AddWorkspaceToAccount(Workspace workspace, Account account) =>
        WriteTuple($"Account:{account.Id}", "parent", $"Workspace:{workspace.Id}");

    public Task RemoveWorkspaceFromAccount(Workspace workspace, Account account) =>
        DeleteTuple($"Account:{account.Id}", "parent", $"Workspace:{workspace.Id}");

    public Task AddUserTo(Workspace workspace, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Workspace:{workspace.Id}");

    public Task RemoveUserFrom(Workspace workspace, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"Workspace:{workspace.Id}");

    public Task AddUserTo(Workspace workspace, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"Workspace:{workspace.Id}", duration);

    // === Policy relations ===
    public Task AddPolicyToWorkspace(Policy policy, Workspace workspace) =>
        WriteTuple($"Workspace:{workspace.Id}", "parent", $"PpgPolicy:{policy.Id}");

    public Task RemovePolicyFromWorkspace(Policy policy, Workspace workspace) =>
        DeleteTuple($"Workspace:{workspace.Id}", "parent", $"PpgPolicy:{policy.Id}");

    public Task AddUserTo(Policy policy, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"PpgPolicy:{policy.Id}");

    public Task RemoveUserFrom(Policy policy, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"PpgPolicy:{policy.Id}");

    public Task AddUserTo(Policy policy, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"PpgPolicy:{policy.Id}", duration);

    // === Configuration relations ===
    public Task AddConfigurationToWorkspace(Configuration config, Workspace workspace) =>
        WriteTuple($"Workspace:{workspace.Id}", "parent", $"CmpConfiguration:{config.Id}");

    public Task RemoveConfigurationFromWorkspace(Configuration config, Workspace workspace) =>
        DeleteTuple($"Workspace:{workspace.Id}", "parent", $"CmpConfiguration:{config.Id}");

    public Task AddUserTo(Configuration config, User user, Role role) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"CmpConfiguration:{config.Id}");

    public Task RemoveUserFrom(Configuration config, User user, Role role) =>
        DeleteTuple($"User:{user.Id}", RoleToRelation(role), $"CmpConfiguration:{config.Id}");

    public Task AddUserTo(Configuration config, User user, Role role, TimeSpan duration) =>
        WriteTuple($"User:{user.Id}", RoleToRelation(role), $"CmpConfiguration:{config.Id}", duration);


    public async Task<bool?> CheckAccess(User user, Role role, object obj)
    {
        try
        {
            var objectType = GetFgaObjectType(obj);
            var objectId = (obj as dynamic).Id;
            var checkRequest = new ClientCheckRequest
            {
                User = $"User:{user.Id}", 
                Relation = role.ToString().ToLower(), 
                Object = $"{objectType}:{objectId}", 
                Context = new
                {
                    current_time = DateTime.Now 
                    
                }
            };
            
            var checkTask = _fgaClient.Check(checkRequest);
            var completedTask = await Task.WhenAny(checkTask, Task.Delay(10000));
            if (completedTask != checkTask)
            {
                Console.WriteLine($"Timeout checking access for {user.Id} {role} on {obj}");
                return null;
            }

            await Task.Delay(100);
            return checkTask.Result.Allowed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR checking access for {user.Id} {role} on {obj}: {ex.Message}");
            return null;
        }
    }

    private string GetFgaObjectType<T>(T obj)
    {
        return obj switch
        {
            Policy => "PpgPolicy",
            Configuration => "CmpConfiguration",
            Workspace => "Workspace",
            Account => "Account",
            User => "User",
            _ => throw new ArgumentException("Ukendt type", nameof(obj))
        };
    }
}