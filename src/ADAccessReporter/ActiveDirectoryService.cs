using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace ADAccessReporter;

public sealed class ActiveDirectoryService
{
    public Task<IReadOnlyList<AdGroupMemberRecord>> GetGroupMembersAsync(
        IEnumerable<string> groupInputs,
        GroupLookupOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<AdGroupMemberRecord>>(
            () => GetGroupMembers(groupInputs, options, progress, cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<AdGroupMemberRecord> GetGroupMembers(
        IEnumerable<string> groupInputs,
        GroupLookupOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<AdGroupMemberRecord>();

        foreach (var rawInput in groupInputs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupInput = rawInput.Trim();
            if (string.IsNullOrWhiteSpace(groupInput))
            {
                continue;
            }

            try
            {
                var identity = IdentityParts.From(groupInput, options.DomainOrServer);
                progress?.Report($"Resolving AD group '{groupInput}'...");

                using var context = CreateDomainContext(identity.ContextName);
                using var group = FindGroup(context, identity.SearchIdentity);

                if (group is null)
                {
                    progress?.Report($"Group not found: {groupInput}");
                    continue;
                }

                var resolvedGroupName = FirstNonEmpty(group.SamAccountName, group.Name, groupInput);
                progress?.Report($"Loading {(options.Recursive ? "recursive" : "direct")} members for '{resolvedGroupName}'...");

                var seenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var members = group.GetMembers(options.Recursive);

                foreach (var principal in members)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (principal is not UserPrincipal user)
                    {
                        principal.Dispose();
                        continue;
                    }

                    using (user)
                    {
                        var record = CreateRecord(groupInput, resolvedGroupName, user);
                        if (!options.IncludeDisabled && string.Equals(record.Enabled, "False", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (seenUsers.Add(record.ComparisonKey))
                        {
                            results.Add(record);
                        }
                    }
                }

                progress?.Report($"Loaded {seenUsers.Count:N0} user(s) from '{resolvedGroupName}'.");
            }
            catch (Exception ex)
            {
                progress?.Report($"Failed to load '{groupInput}': {ex.Message}");
            }
        }

        return results;
    }

    internal static PrincipalContext CreateDomainContext(string contextName)
    {
        return string.IsNullOrWhiteSpace(contextName)
            ? new PrincipalContext(ContextType.Domain)
            : new PrincipalContext(ContextType.Domain, contextName);
    }

    internal static GroupPrincipal? FindGroup(PrincipalContext context, string identity)
    {
        foreach (var identityType in GetIdentitySearchOrder(identity))
        {
            try
            {
                var group = GroupPrincipal.FindByIdentity(context, identityType, identity);
                if (group is not null)
                {
                    return group;
                }
            }
            catch (MultipleMatchesException)
            {
                throw;
            }
            catch
            {
                // Keep trying other identity formats.
            }
        }

        try
        {
            return GroupPrincipal.FindByIdentity(context, identity);
        }
        catch (MultipleMatchesException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    internal static Principal? FindPrincipal(string identity, string defaultDomainOrServer)
    {
        var parts = IdentityParts.From(identity, defaultDomainOrServer);
        if (IsWellKnownLocalAuthority(parts.ContextName))
        {
            return null;
        }

        try
        {
            using var context = CreateContextForPrincipal(parts.ContextName);
            var sid = TryTranslateToSid(identity);

            if (!string.IsNullOrWhiteSpace(sid))
            {
                try
                {
                    var bySid = Principal.FindByIdentity(context, IdentityType.Sid, sid);
                    if (bySid is not null)
                    {
                        return bySid;
                    }
                }
                catch
                {
                    // Fall through to name-based lookup.
                }
            }

            foreach (var identityType in GetIdentitySearchOrder(parts.SearchIdentity))
            {
                try
                {
                    var principal = Principal.FindByIdentity(context, identityType, parts.SearchIdentity);
                    if (principal is not null)
                    {
                        return principal;
                    }
                }
                catch
                {
                    // Keep trying other identity formats.
                }
            }

            return Principal.FindByIdentity(context, parts.SearchIdentity);
        }
        catch
        {
            return null;
        }
    }

    internal static string GetPrincipalType(string identity, string defaultDomainOrServer)
    {
        if (IsWellKnownIdentity(identity))
        {
            return "Built-in";
        }

        using var principal = FindPrincipal(identity, defaultDomainOrServer);
        return principal switch
        {
            UserPrincipal => "User",
            GroupPrincipal => "Group",
            ComputerPrincipal => "Computer",
            null => "Unresolved",
            _ => FirstNonEmpty(principal.StructuralObjectClass, principal.GetType().Name)
        };
    }

    internal static IReadOnlyList<AdGroupMemberRecord> GetExpandedGroupUsers(
        string groupIdentity,
        string defaultDomainOrServer,
        bool includeDisabledUsers,
        CancellationToken cancellationToken)
    {
        var parts = IdentityParts.From(groupIdentity, defaultDomainOrServer);
        if (IsWellKnownLocalAuthority(parts.ContextName))
        {
            return Array.Empty<AdGroupMemberRecord>();
        }

        var records = new List<AdGroupMemberRecord>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var context = CreateContextForPrincipal(parts.ContextName);
        using var group = FindGroup(context, parts.SearchIdentity);
        if (group is null)
        {
            return records;
        }

        using var members = group.GetMembers(true);
        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is not UserPrincipal user)
            {
                member.Dispose();
                continue;
            }

            using (user)
            {
                var record = CreateRecord(groupIdentity, groupIdentity, user);
                if (!includeDisabledUsers && string.Equals(record.Enabled, "False", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (seen.Add(record.ComparisonKey))
                {
                    records.Add(record);
                }
            }
        }

        return records;
    }

    internal static AdGroupMemberRecord CreateRecord(string inputGroup, string groupName, UserPrincipal user)
    {
        var sid = user.Sid?.Value ?? string.Empty;
        var sam = user.SamAccountName ?? string.Empty;
        var upn = user.UserPrincipalName ?? string.Empty;
        var distinguishedName = user.DistinguishedName ?? ReadDirectoryProperty(user, "distinguishedName");
        var displayName = FirstNonEmpty(user.DisplayName, ReadDirectoryProperty(user, "displayName"), user.Name);
        var email = FirstNonEmpty(user.EmailAddress, ReadDirectoryProperty(user, "mail"));
        var enabled = user.Enabled.HasValue ? (user.Enabled.Value ? "True" : "False") : string.Empty;

        return new AdGroupMemberRecord
        {
            InputGroup = inputGroup,
            GroupName = groupName,
            Name = user.Name ?? string.Empty,
            SamAccountName = sam,
            DisplayName = displayName,
            UserPrincipalName = upn,
            Email = email,
            Enabled = enabled,
            DistinguishedName = distinguishedName,
            Sid = sid,
            ComparisonKey = FirstNonEmpty(sid, upn, sam, distinguishedName, user.Name).ToUpperInvariant()
        };
    }

    private static PrincipalContext CreateContextForPrincipal(string contextName)
    {
        if (!string.IsNullOrWhiteSpace(contextName) &&
            string.Equals(contextName, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
        {
            return new PrincipalContext(ContextType.Machine, contextName);
        }

        return CreateDomainContext(contextName);
    }

    private static IEnumerable<IdentityType> GetIdentitySearchOrder(string identity)
    {
        if (identity.StartsWith("CN=", StringComparison.OrdinalIgnoreCase) ||
            identity.StartsWith("OU=", StringComparison.OrdinalIgnoreCase))
        {
            yield return IdentityType.DistinguishedName;
        }

        if (Guid.TryParse(identity, out _))
        {
            yield return IdentityType.Guid;
        }

        if (identity.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
        {
            yield return IdentityType.Sid;
        }

        yield return IdentityType.SamAccountName;
        yield return IdentityType.Name;
        yield return IdentityType.UserPrincipalName;
        yield return IdentityType.DistinguishedName;
    }

    private static string ReadDirectoryProperty(Principal principal, string propertyName)
    {
        try
        {
            if (principal.GetUnderlyingObject() is DirectoryEntry entry &&
                entry.Properties.Contains(propertyName))
            {
                return entry.Properties[propertyName].Value?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // Optional directory attributes are best-effort only.
        }

        return string.Empty;
    }

    private static string TryTranslateToSid(string identity)
    {
        try
        {
            var account = new System.Security.Principal.NTAccount(identity);
            return account.Translate(typeof(System.Security.Principal.SecurityIdentifier)).Value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsWellKnownIdentity(string identity)
    {
        var normalized = identity.Trim();
        return normalized.Equals("Everyone", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("CREATOR OWNER", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("NT AUTHORITY\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("BUILTIN\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("APPLICATION PACKAGE AUTHORITY\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("ALL APPLICATION PACKAGES", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWellKnownLocalAuthority(string contextName)
    {
        return contextName.Equals("NT AUTHORITY", StringComparison.OrdinalIgnoreCase) ||
               contextName.Equals("BUILTIN", StringComparison.OrdinalIgnoreCase) ||
               contextName.Equals("APPLICATION PACKAGE AUTHORITY", StringComparison.OrdinalIgnoreCase);
    }

    internal static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private sealed class IdentityParts
    {
        public string ContextName { get; private init; } = string.Empty;
        public string SearchIdentity { get; private init; } = string.Empty;

        public static IdentityParts From(string identity, string defaultDomainOrServer)
        {
            var trimmed = identity.Trim();
            var context = defaultDomainOrServer.Trim();
            var searchIdentity = trimmed;

            if (!trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                var slashIndex = trimmed.IndexOf('\\');
                if (slashIndex > 0 && slashIndex < trimmed.Length - 1)
                {
                    context = trimmed[..slashIndex];
                    searchIdentity = trimmed[(slashIndex + 1)..];
                }
            }

            return new IdentityParts
            {
                ContextName = context,
                SearchIdentity = searchIdentity
            };
        }
    }
}
