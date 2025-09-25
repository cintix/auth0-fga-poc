using FGA_POC.Dtos;

namespace FGA_POC;

public class TestScenario
{
    public User Bob { get; } = new("Bob");
    public User Sara { get; } = new("Sara");
    public User Jenny { get; } = new("Jenny");
    public User Geo { get; } = new("Geo");
    
    public Account Account1 { get; } = new("1");
    public Account Account2 { get; } = new("2");

    public Workspace Workspace1 { get; } = new("1");
    public Workspace Workspace2 { get; } = new("2");

    public Policy Policy1A { get; } = new("1a");
    public Policy Policy1B { get; } = new("1b");
    public Policy Policy2A { get; } = new("2a");
    public Policy Policy2B { get; } = new("2b");

    public Configuration Config1A { get; } = new("1a");
    public Configuration Config1B { get; } = new("1b");
    public Configuration Config2A { get; } = new("2a");
    public Configuration Config2B { get; } = new("2b");
}

public static class TestScenarioHelper
{
    public static async Task<TestScenario> SetupScenario(IAuth0FgaService fgaService)
    {
        var s = new TestScenario();

        // Accounts
        await fgaService.AddAccountToUser(s.Account1, s.Bob);
        await fgaService.AddAccountToUser(s.Account2, s.Sara);

        // Workspaces
        await fgaService.AddWorkspaceToAccount(s.Workspace1, s.Account1);
        await fgaService.AddWorkspaceToAccount(s.Workspace2, s.Account2);

        // Policies
        await fgaService.AddPolicyToWorkspace(s.Policy1A, s.Workspace1);
        await fgaService.AddPolicyToWorkspace(s.Policy1B, s.Workspace1);
        await fgaService.AddPolicyToWorkspace(s.Policy2A, s.Workspace2);
        await fgaService.AddPolicyToWorkspace(s.Policy2B, s.Workspace2);

        // Configurations
        await fgaService.AddConfigurationToWorkspace(s.Config1A, s.Workspace1);
        await fgaService.AddConfigurationToWorkspace(s.Config1B, s.Workspace1);
        await fgaService.AddConfigurationToWorkspace(s.Config2A, s.Workspace2);
        await fgaService.AddConfigurationToWorkspace(s.Config2B, s.Workspace2);

        // Direct user roles
        await fgaService.AddUserTo(s.Workspace1, s.Jenny, Role.Editor);
        await fgaService.AddUserTo(s.Workspace2, s.Geo, Role.Reader);
        await fgaService.AddUserTo(s.Config2A, s.Bob, Role.Reviewer);
        await fgaService.AddUserTo(s.Policy1A, s.Sara, Role.Editor);

        return s;
    }

    public static async Task CleanupScenario(IAuth0FgaService fgaService, TestScenario s)
    {
        // Remove direct roles
        await fgaService.RemoveUserFrom(s.Policy1A, s.Sara, Role.Editor);
        await fgaService.RemoveUserFrom(s.Config2A, s.Bob, Role.Reviewer);
        await fgaService.RemoveUserFrom(s.Workspace2, s.Geo, Role.Reader);
        await fgaService.RemoveUserFrom(s.Workspace1, s.Jenny, Role.Editor);


        // Remove configurations
        await fgaService.RemoveConfigurationFromWorkspace(s.Config2B, s.Workspace2);
        await fgaService.RemoveConfigurationFromWorkspace(s.Config2A, s.Workspace2);
        await fgaService.RemoveConfigurationFromWorkspace(s.Config1B, s.Workspace1);
        await fgaService.RemoveConfigurationFromWorkspace(s.Config1A, s.Workspace1);

        // Remove policies
        await fgaService.RemovePolicyFromWorkspace(s.Policy2B, s.Workspace2);
        await fgaService.RemovePolicyFromWorkspace(s.Policy2A, s.Workspace2);
        await fgaService.RemovePolicyFromWorkspace(s.Policy1B, s.Workspace1);
        await fgaService.RemovePolicyFromWorkspace(s.Policy1A, s.Workspace1);

        // Remove workspaces
        await fgaService.RemoveWorkspaceFromAccount(s.Workspace2, s.Account2);
        await fgaService.RemoveWorkspaceFromAccount(s.Workspace1, s.Account1);

        // Remove accounts
        await fgaService.RemoveAccountFromUser(s.Account2, s.Sara);
        await fgaService.RemoveAccountFromUser(s.Account1, s.Bob);
    }
}
