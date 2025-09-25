using Microsoft.Extensions.Configuration;

namespace FGA_POC;

using OpenFga.Sdk.Client;
using Dtos;

class Program
{
    static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var fgaSettings = configuration.GetSection("Fga").Get<FgaSettings>();

        var clientConfig = new ClientConfiguration()
        {
            ApiUrl = fgaSettings.ApiUrl,
            StoreId = fgaSettings.StoreId,
            AuthorizationModelId = fgaSettings.AuthorizationModelId,
            Credentials = new OpenFga.Sdk.Configuration.Credentials()
            {
                Method = OpenFga.Sdk.Configuration.CredentialsMethod.ClientCredentials,
                Config = new OpenFga.Sdk.Configuration.CredentialsConfig()
                {
                    ApiTokenIssuer = fgaSettings.ApiTokenIssuer,
                    ApiAudience = fgaSettings.ApiAudience,
                    ClientId = fgaSettings.ClientId,
                    ClientSecret = fgaSettings.ClientSecret,
                }
            }
        };

        IAuth0FgaService fgaService = new Auth0FgaService(clientConfig);

        await TestUserRolePermissions(fgaService);
        await TestTemporaryUserRolePermissions(fgaService);
    }

    private static async Task TestUserRolePermissions(IAuth0FgaService fgaService)
    {
        var scenario = await TestScenarioHelper.SetupScenario(fgaService);

        try
        {
            var users = new Dictionary<string, User>
            {
                ["Bob"] = scenario.Bob,
                ["Sara"] = scenario.Sara,
                ["Jenny"] = scenario.Jenny,
                ["Geo"] = scenario.Geo
            };
            var roles = new[]
            {
                Role.Admin,
                Role.Editor,
                Role.Reader,
                Role.Reviewer
            };
            await CheckUserRoleAccessOnResources(fgaService, scenario, users, roles);
        }
        finally
        {
            await TestScenarioHelper.CleanupScenario(fgaService, scenario);
        }
    }

    private static async Task CheckUserRoleAccessOnResources(IAuth0FgaService fgaService, TestScenario scenario, Dictionary<string, User> users, Role[] roles)
    {
        var resources = new Dictionary<string, object>
        {
            ["Account1"] = scenario.Account1,
            ["Account2"] = scenario.Account2,
            ["Workspace1"] = scenario.Workspace1,
            ["Workspace2"] = scenario.Workspace2,
            ["Policy1a"] = scenario.Policy1A,
            ["Policy1b"] = scenario.Policy1B,
            ["Policy2a"] = scenario.Policy2A,
            ["Policy2b"] = scenario.Policy2B,
            ["Config1a"] = scenario.Config1A,
            ["Config1b"] = scenario.Config1B,
            ["Config2a"] = scenario.Config2A,
            ["Config2b"] = scenario.Config2B
        };
        foreach (var userPair in users)
        {
            string userName = userPair.Key;
            User user = userPair.Value;
            Console.WriteLine($"\n---- ACCESS CHECKS for {userName} ----");
            foreach (var role in roles)
            {
                Console.WriteLine($"\nRole: {role}");
                foreach (var resourcePair in resources)
                {
                    string resourceName = resourcePair.Key;
                    var resource = resourcePair.Value;
                    if (role == Role.Reviewer && resource is not Configuration) continue;

                    bool? hasAccess = await fgaService.CheckAccess(user, role, resource);
                    string symbol = ToResultSymbol(hasAccess);
                    Console.WriteLine($"{userName} {role} on {resourceName}: {symbol}");
                }
            }
        }
    }

    private static async Task TestTemporaryUserRolePermissions(IAuth0FgaService fgaService)
    {
        var scenario = await TestScenarioHelper.SetupScenario(fgaService);

        try
        {
            var tempGrants = new List<(User user, Role role, Role[] validationRoles, object resource, TimeSpan duration)>
            {
                (scenario.Geo, Role.Admin, [Role.Admin, Role.Editor, Role.Reader], scenario.Account1, TimeSpan.FromSeconds(10)),
                (scenario.Jenny, Role.Editor, [Role.Editor, Role.Reader], scenario.Workspace2, TimeSpan.FromSeconds(10)),
                (scenario.Geo, Role.Reviewer, [Role.Reviewer, Role.Editor], scenario.Config1A, TimeSpan.FromSeconds(10)),
                (scenario.Bob, Role.Admin, [Role.Admin, Role.Editor, Role.Reader], scenario.Account2, TimeSpan.FromSeconds(10)),
            };

            foreach (var grant in tempGrants)
            {
                Console.WriteLine($"\nAdding temporary {grant.role} to {grant.user.Id} for {grant.duration.TotalSeconds} seconds on {GetObjectId(grant.resource)}...");
                await AddTemporaryUserTo(fgaService, grant.resource, grant.user, grant.role, grant.duration);

                bool? hasAccessNow = await fgaService.CheckAccess(grant.user, grant.role, grant.resource);
                Console.WriteLine($"Immediate access: {ToResultSymbol(hasAccessNow)}");

                await CheckUserRoleAccessOnResources(fgaService, scenario, new() { [grant.user.Id] = grant.user }, grant.validationRoles);

                Console.WriteLine("Waiting for temporary grant to expire...");
                await Task.Delay(grant.duration);

                bool? hasAccessLater = await fgaService.CheckAccess(grant.user, grant.role, grant.resource);
                Console.WriteLine($"Access after expiration: {ToResultSymbol(hasAccessLater)}");

                await CheckUserRoleAccessOnResources(fgaService, scenario, new() { [grant.user.Id] = grant.user }, grant.validationRoles);

                await RemoveTemporaryUserFrom(fgaService, grant.resource, grant.user, grant.role);
            }
        }
        finally
        {
            await TestScenarioHelper.CleanupScenario(fgaService, scenario);
        }
    }

    private static async Task AddTemporaryUserTo(IAuth0FgaService fgaService, object resource, User user, Role role, TimeSpan duration)
    {
        switch (resource)
        {
            case Account a: await fgaService.AddUserTo(a, user, role, duration); break;
            case Workspace w: await fgaService.AddUserTo(w, user, role, duration); break;
            case Policy p: await fgaService.AddUserTo(p, user, role, duration); break;
            case Configuration c: await fgaService.AddUserTo(c, user, role, duration); break;
            default: throw new ArgumentException("Unknown resource type");
        }
    }

    private static async Task RemoveTemporaryUserFrom(IAuth0FgaService fgaService, object resource, User user, Role role)
    {
        switch (resource)
        {
            case Account a: await fgaService.RemoveUserFrom(a, user, role); break;
            case Workspace w: await fgaService.RemoveUserFrom(w, user, role); break;
            case Policy p: await fgaService.RemoveUserFrom(p, user, role); break;
            case Configuration c: await fgaService.RemoveUserFrom(c, user, role); break;
            default: throw new ArgumentException("Unknown resource type");
        }
    }

    private static string GetObjectId(object obj) => (obj as dynamic).Id;

    private static string ToResultSymbol(bool? value)
    {
        if (value == true)
            return "\u001b[32mTrue\u001b[0m";
        if (value == false)
            return "\u001b[31mFalse\u001b[0m";
        return "\u001b[33m?\u001b[0m";
    }
}