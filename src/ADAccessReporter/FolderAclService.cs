using System.DirectoryServices.AccountManagement;
using System.Security.AccessControl;

namespace ADAccessReporter;

public sealed class FolderAclService
{
    public Task<IReadOnlyList<FolderRightsRecord>> GetRightsAsync(
        string path,
        FolderRightsOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<FolderRightsRecord>>(
            () => GetRights(path, options, progress, cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<FolderRightsRecord> GetRights(
        string path,
        FolderRightsOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            throw new DirectoryNotFoundException($"The path does not exist or is not reachable: {path}");
        }

        progress?.Report($"Reading NTFS permissions for '{path}'...");
        FileSystemSecurity security = Directory.Exists(path)
            ? new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access)
            : new FileInfo(path).GetAccessControl(AccessControlSections.Access);

        var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
        var records = new List<FolderRightsRecord>();

        foreach (FileSystemAccessRule rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!options.IncludeInherited && rule.IsInherited)
            {
                continue;
            }

            if (!options.IncludeDenyEntries && rule.AccessControlType == AccessControlType.Deny)
            {
                continue;
            }

            var identity = rule.IdentityReference.Value;
            var principalType = ActiveDirectoryService.GetPrincipalType(identity, options.DomainOrServer);
            records.Add(CreateRawRecord(path, rule, identity, principalType));

            if (options.ExpandAdGroups && string.Equals(principalType, "Group", StringComparison.OrdinalIgnoreCase))
            {
                records.AddRange(ExpandGroupRecord(path, rule, identity, options, progress, cancellationToken));
            }
        }

        progress?.Report($"Loaded {records.Count:N0} permission row(s) for '{path}'.");
        return records;
    }

    private static FolderRightsRecord CreateRawRecord(string path, FileSystemAccessRule rule, string identity, string principalType)
    {
        return new FolderRightsRecord
        {
            Path = path,
            EntryKind = "ACL entry",
            Identity = identity,
            PrincipalType = principalType,
            AccessType = rule.AccessControlType.ToString(),
            Rights = rule.FileSystemRights.ToString(),
            IsInherited = rule.IsInherited ? "True" : "False",
            Inheritance = rule.InheritanceFlags.ToString(),
            Propagation = rule.PropagationFlags.ToString()
        };
    }

    private static IEnumerable<FolderRightsRecord> ExpandGroupRecord(
        string path,
        FileSystemAccessRule rule,
        string identity,
        FolderRightsOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var expanded = new List<FolderRightsRecord>();

        try
        {
            using var principal = ActiveDirectoryService.FindPrincipal(identity, options.DomainOrServer);
            if (principal is not GroupPrincipal group)
            {
                return expanded;
            }

            progress?.Report($"Expanding ACL group '{identity}'...");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    var userRecord = ActiveDirectoryService.CreateRecord(identity, identity, user);
                    if (!options.IncludeDisabledUsers &&
                        string.Equals(userRecord.Enabled, "False", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!seen.Add(userRecord.ComparisonKey))
                    {
                        continue;
                    }

                    expanded.Add(new FolderRightsRecord
                    {
                        Path = path,
                        EntryKind = "Expanded group member",
                        Identity = identity,
                        PrincipalType = "User via group",
                        AccessType = rule.AccessControlType.ToString(),
                        Rights = rule.FileSystemRights.ToString(),
                        IsInherited = rule.IsInherited ? "True" : "False",
                        Inheritance = rule.InheritanceFlags.ToString(),
                        Propagation = rule.PropagationFlags.ToString(),
                        ExpandedUserName = userRecord.Name,
                        ExpandedSamAccountName = userRecord.SamAccountName,
                        ExpandedDisplayName = userRecord.DisplayName,
                        ExpandedEmail = userRecord.Email,
                        ExpandedUserPrincipalName = userRecord.UserPrincipalName,
                        ExpandedSid = userRecord.Sid
                    });
                }
            }

            progress?.Report($"Expanded {expanded.Count:N0} user(s) from '{identity}'.");
        }
        catch (Exception ex)
        {
            expanded.Add(new FolderRightsRecord
            {
                Path = path,
                EntryKind = "Expansion error",
                Identity = identity,
                PrincipalType = "Group",
                AccessType = rule.AccessControlType.ToString(),
                Rights = rule.FileSystemRights.ToString(),
                IsInherited = rule.IsInherited ? "True" : "False",
                Inheritance = rule.InheritanceFlags.ToString(),
                Propagation = rule.PropagationFlags.ToString(),
                Notes = ex.Message
            });
            progress?.Report($"Could not expand ACL group '{identity}': {ex.Message}");
        }

        return expanded;
    }
}
