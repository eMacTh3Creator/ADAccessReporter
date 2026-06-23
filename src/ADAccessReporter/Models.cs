namespace ADAccessReporter;

public sealed class GroupLookupOptions
{
    public string DomainOrServer { get; set; } = string.Empty;
    public bool Recursive { get; set; } = true;
    public bool IncludeDisabled { get; set; }
}

public sealed class FolderRightsOptions
{
    public string DomainOrServer { get; set; } = string.Empty;
    public bool ExpandAdGroups { get; set; } = true;
    public bool IncludeInherited { get; set; } = true;
    public bool IncludeDenyEntries { get; set; } = true;
    public bool IncludeDisabledUsers { get; set; }
}

public sealed class AdGroupMemberRecord
{
    public string InputGroup { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SamAccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Enabled { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;
    public string ComparisonKey { get; set; } = string.Empty;
}

public sealed class FolderRightsRecord
{
    public string Path { get; set; } = string.Empty;
    public string EntryKind { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;
    public string PrincipalType { get; set; } = string.Empty;
    public string AccessType { get; set; } = string.Empty;
    public string Rights { get; set; } = string.Empty;
    public string IsInherited { get; set; } = string.Empty;
    public string Inheritance { get; set; } = string.Empty;
    public string Propagation { get; set; } = string.Empty;
    public string ExpandedUserName { get; set; } = string.Empty;
    public string ExpandedSamAccountName { get; set; } = string.Empty;
    public string ExpandedDisplayName { get; set; } = string.Empty;
    public string ExpandedEmail { get; set; } = string.Empty;
    public string ExpandedUserPrincipalName { get; set; } = string.Empty;
    public string ExpandedSid { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
