<p align="center">
  <img src="assets/ad-access-reporter.png" alt="AD Access Reporter logo" width="96" height="96">
</p>

<h1 align="center">AD Access Reporter</h1>

<p align="center">
  <a href="https://emacth3creator.github.io/ADAccessReporter/">Website</a>
  ·
  <a href="https://github.com/eMacTh3Creator/ADAccessReporter/releases/latest">Latest release</a>
</p>

Windows desktop utility for Active Directory group reporting, group-to-group comparison, and folder permission exports.

## What it does

- Loads users from one or more AD groups.
- Supports nested group membership.
- Compares multiple groups and flags users who are common, unique, or missing.
- Exports group membership and comparison results to CSV.
- Reads NTFS permissions from a local folder, file, or UNC path.
- Optionally expands AD groups found in folder ACLs so you can see the users behind a permission entry.
- Exports folder rights to CSV.

## Requirements

- Windows 10 or later.
- Network/domain access to Active Directory.
- Permission to read the requested AD groups and filesystem ACLs.
- .NET 8 Desktop Runtime for the default portable build.

The app uses your current Windows credentials. If needed, enter a domain or domain controller in the optional field.

## Quick start

1. Download `ADAccessReporter-v0.1.0-win-x64.exe` from the release folder or GitHub release.
2. Open the app on a domain-joined or domain-connected Windows machine.
3. Enter one AD group per line and click **Load Groups**.
4. Review the **Members** and **Comparison** tabs.
5. Use the export buttons to save CSV files.

For folder permissions, paste a path like `\\server\share\folder`, choose whether to expand AD groups in the ACL, and click **Scan Rights**.

## Important note about share permissions

The folder scanner reports NTFS permissions visible from the selected path. Windows share-level permissions are separate from NTFS ACLs and usually require server-side administrative tooling to inspect.

## Build

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\Build-Release.ps1
```

Artifacts are written to `release\`:

- `ADAccessReporter-v0.1.0-win-x64.exe`
- `ADAccessReporter-v0.1.0-portable.zip`
- `latest.json`
- `checksums.txt`

To build a larger self-contained executable that does not require the .NET Desktop Runtime, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\Build-Release.ps1 -SelfContained
```

## GitHub Pages

The root `index.html` is a static GitHub Pages site with download links and release details. The included workflow deploys the site on pushes to `main` or `master`.

## License

MIT
