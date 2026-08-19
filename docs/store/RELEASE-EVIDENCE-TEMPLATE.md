# Waller Microsoft Store release evidence

Copy this file for each Store submission. Keep private account screenshots, credentials, and certificate private keys outside the repository.

## Identity

| Field | Value |
| --- | --- |
| Product | Waller |
| Store ID | `[value]` |
| Package identity name | `[value]` |
| Publisher | `[value]` |
| Publisher display name | `[value]` |
| PFN | `[verification value]` |
| Package SID | `[verification value]` |
| Package version | `[0.0.0.0]` |
| Architecture(s) | `[x64/x86/ARM64]` |
| Source commit | `[SHA]` |
| Source tag | `[tag/n-a]` |
| Build date UTC | `[timestamp]` |

## Artifact

| Field | Value |
| --- | --- |
| Upload file | `[name.msixupload]` |
| Size | `[bytes]` |
| SHA-256 | `[hash]` |
| Target device family | `Windows.Desktop` |
| Minimum OS | `10.0.17763.0` |
| Restricted capabilities | `runFullTrust` |
| Application ID | `App` |
| Source workflow/command | `[reference]` |

## Automated gates

| Check | Command/run | Result | Evidence |
| --- | --- | --- | --- |
| Store structure | `.\native\scripts\TestStoreReadiness.ps1 -RequireReservedIdentity` | `[pass/fail]` | |
| Full native verify | `.\scripts\Invoke-Native.ps1 -Task Verify` | `[pass/fail]` | |
| Surface smoke | `.\scripts\Invoke-Native.ps1 -Task Verify -SurfaceSmoke -SettingsRoundTrip` | `[pass/fail]` | |
| Apply smoke | `.\scripts\Invoke-Native.ps1 -Task Verify -ApplySmoke` | `[pass/fail]` | |
| Store upload build | `.\native\scripts\BuildStoreUpload.ps1 -Platform x64` | `[pass/fail]` | |

## Test environment

| Field | Value |
| --- | --- |
| Windows edition/version/build | `[value]` |
| Architecture | `[value]` |
| Clean VM/profile | `[yes/no]` |
| Developer tools installed | `[yes/no]` |
| Account type | `[standard/admin]` |
| Monitor count | `[number]` |
| Monitor resolutions/scales | `[values]` |

## Lifecycle and functional matrix

| Scenario | Result | Evidence/notes |
| --- | --- | --- |
| Clean install | `[pass/fail]` | |
| Start-menu launch | `[pass/fail]` | |
| Single-monitor discovery | `[pass/fail]` | |
| Multi-monitor discovery | `[pass/fail/n-a]` | |
| Local image source | `[pass/fail]` | |
| Solid-color source | `[pass/fail]` | |
| Empty assignment | `[pass/fail]` | |
| Cover | `[pass/fail]` | |
| Contain | `[pass/fail]` | |
| Stretch | `[pass/fail]` | |
| Center | `[pass/fail]` | |
| Tile | `[pass/fail]` | |
| Save preset without Apply | `[pass/fail]` | |
| Load/rename/duplicate/delete preset | `[pass/fail]` | |
| Apply all | `[pass/fail]` | starting and final paths recorded |
| Apply cancellation | `[pass/fail]` | |
| Settings round trip | `[pass/fail]` | |
| English UI | `[pass/fail]` | |
| Spanish UI | `[pass/fail]` | |
| Upgrade from previous Store version | `[pass/fail/n-a]` | |
| Presets/settings preserved | `[pass/fail/n-a]` | |
| Uninstall | `[pass/fail]` | |
| Original images untouched | `[pass/fail]` | |
| Reinstall | `[pass/fail]` | |
| Standard-user operation | `[pass/fail]` | |
| Offline operation | `[pass/fail]` | |

## Wallpaper safety evidence

- Starting wallpaper path(s): `[values]`
- Applied rendered path(s): `[values]`
- Restored wallpaper path(s): `[values]`
- Restore completed in failure/cancellation path: `[yes/no]`
- Original source hashes unchanged: `[yes/no]`
- Render output location: `%USERPROFILE%\.waller\rendered`

## Privacy/security

- [ ] Public privacy URL loads without authentication.
- [ ] Privacy policy matches current storage and network behavior.
- [ ] Promotional images are licensed and non-sensitive.
- [ ] Screenshots contain no personal paths or private images.
- [ ] Logs contain no image content or unnecessary personal paths.
- [ ] No `.pfx`, private key, or password is in the artifact/repository.
- [ ] Normal use succeeds without elevation.
- [ ] Protected paths fail safely.

## Partner Center

### Pricing and availability

- Markets: `[selection]`
- Audience: `[selection]`
- Discoverability: `[selection]`
- Schedule: `[selection]`
- Base price: `[selection]`
- Publishing hold: `[selection]`

### Properties

- Category/subcategory: `[selection]`
- Privacy URL: `[url]`
- Website: `[url]`
- Support: `[url/email]`
- Contact details complete: `[yes/no]`
- Requirements complete: `[yes/no]`

### Age ratings

- Questionnaire complete: `[yes/no]`
- Assigned rating: `[value]`

### Packages

- Upload validation: `[result]`
- Packages section complete: `[yes/no]`
- Device family: `[Windows Desktop only]`
- Warnings: `[none/list]`

### Listings

- English listing reviewed: `[yes/no]`
- Spanish listing reviewed: `[yes/no]`
- What's new updated: `[yes/no]`
- Screenshot count: `[number]`
- Artwork rights verified: `[yes/no]`

### Submission options

- Notes date: `[date]`
- Apply mutation warning included: `[yes/no]`
- `runFullTrust` explanation entered: `[yes/no]`
- Notification audience reviewed: `[yes/no]`
- Publishing hold confirmed: `[yes/no]`

## Certification outcome

| Field | Value |
| --- | --- |
| Submitted | `[timestamp]` |
| Result | `[passed/failed/cancelled]` |
| Findings | `[summary]` |
| Remediation | `[commit/submission]` |
| Approved | `[timestamp]` |
| Published/held | `[value]` |
| Live Store URL | `[url]` |
| Deep link | `[value]` |

## Approval

- Engineering: `[name/date]`
- Product/listing: `[name/date]`
- Privacy/security: `[name/date]`
- Publisher owner: `[name/date]`
- Decision: `[publish/hold/reject]`
