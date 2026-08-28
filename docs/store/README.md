# Waller Microsoft Store submission runbook

This directory defines the Store publication contract for Waller's packaged WinUI application.

The Store channel and direct/GitHub channel are separate release products:

```text
Store channel
  -> Partner Center identity
  -> .msixupload preferred
  -> Microsoft Store-managed signing and updates

Direct/GitHub channel
  -> publisher-owned signing
  -> publisher-owned hosting and updates
  -> never represented as the Store artifact
```

## Current status

Waller already has a packaged WinUI application, MSIX assets, a stable application ID, full-trust desktop execution, development signing, package inspection, update-policy guards, accessibility/localization checks, and packaged smoke tests.

The committed identity is still a development identity:

```text
Name:      1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4
Publisher: CN=Waller
Display:   Waller
```

A Store submission must not use these values unless Partner Center independently assigns the exact same values. Reserve the Waller product in Partner Center, then apply the returned values with:

```powershell
powershell -ExecutionPolicy Bypass -File .\native\scripts\SetStoreIdentity.ps1 `
  -Name '<Package/Identity/Name>' `
  -Publisher '<Package/Identity/Publisher>' `
  -PublisherDisplayName '<Package/Properties/PublisherDisplayName>' `
  -StoreId '<Store ID>' `
  -PackageFamilyName '<PFN>' `
  -PackageSid '<Package SID>'
```

The script rejects leading, trailing, and non-breaking whitespace. PFN and Package SID are stored only as verification metadata and are never written into the manifest.

## Desktop-only targeting

Waller is a Windows desktop utility. Its manifest targets only:

```xml
<TargetDeviceFamily Name="Windows.Desktop" ... />
```

Do not restore `Windows.Universal`. Partner Center device-family availability cannot fully compensate for a manifest that claims broader device support than the product can provide.

## Local validation

Run the structural gate at any time:

```powershell
powershell -ExecutionPolicy Bypass -File .\native\scripts\TestStoreReadiness.ps1
```

Before building a submission, require the reserved identity:

```powershell
powershell -ExecutionPolicy Bypass -File .\native\scripts\TestStoreReadiness.ps1 -RequireReservedIdentity
```

Generate a Store upload candidate after reservation:

```powershell
powershell -ExecutionPolicy Bypass -File .\native\scripts\BuildStoreUpload.ps1 -Platform x64
```

The build path uses a short-lived self-signed certificate only to construct and test the Store upload. It is not a public code-signing credential. Microsoft Store replaces MSIX/AppX signatures after certification.

## Partner Center submission checklist

### 1. Pricing and availability

Complete every required field intentionally:

- Markets: review rather than accepting a broad default without consideration.
- Audience: use Public only when the first release is qualified.
- Discoverability: choose discoverable or direct-link-only deliberately.
- Schedule: use a manual publishing hold for the first submission.
- Base price: choose Free unless a separate commercial plan exists.

A manual hold allows certification to finish without immediately exposing a listing or package that has not been reviewed after Store processing.

### 2. Properties

Recommended first-release values:

- Primary category: Personalization or Utilities & tools, based on the current Partner Center taxonomy.
- Privacy policy URL: a stable public rendering of [`../../PRIVACY.md`](../../PRIVACY.md).
- Website: public project/product page.
- Support: issue tracker, support page, or monitored email.
- Display mode: no immersive/XR declarations.
- Minimum OS: Windows 10 version 1809.
- Hardware: one or more Windows displays; multi-monitor setup recommended, not required unless product behavior says otherwise.

Waller reads monitor topology, local image paths, and wallpaper configuration. Publish a privacy policy even if no data is transmitted.

### 3. Age ratings

Complete every question. Waller contains no built-in violence, sexual content, gambling, public user-generated content, or unrestricted browser. User-selected wallpaper files are local content chosen by the user and are not supplied or transmitted by Waller.

### 4. Packages

Prefer the generated `.msixupload` for Partner Center.

Before upload, verify:

- exact Partner Center `Name`, `Publisher`, and `PublisherDisplayName`;
- `Windows.Desktop` is the only target device family;
- package version is greater than all previous applicable submissions;
- architecture matches the listing;
- `runFullTrust` appears as expected;
- app assets resolve;
- package contains no `.pfx`, private key, or certificate password;
- the package comes from the reviewed commit;
- the Store package is not the raw executable ZIP produced by the direct release workflow.

A package may show **Validated** while the Packages section remains **Incomplete**. Complete all device-family and package-related controls before submission.

### 5. Store listings

Prepare English and Spanish listings from [`LISTING.md`](LISTING.md).

At least one screenshot is required by Partner Center; prepare at least four current screenshots:

1. monitor/session overview;
2. per-monitor wallpaper source and placement;
3. preset management;
4. apply progress/result;
5. optional English/Spanish or light/dark appearance screen.

All screenshots must use non-sensitive local paths and images licensed for promotional use.

### 6. Submission options

Use [`CERTIFICATION-NOTES.md`](CERTIFICATION-NOTES.md) as the source.

Waller declares `runFullTrust`, so enter a detailed restricted-capability explanation. The reviewer must understand that Waller uses normal desktop APIs to:

- enumerate connected monitors;
- read current wallpaper assignments;
- read explicitly selected image files;
- render per-monitor PNG files;
- write package-local settings/presets;
- call `IDesktopWallpaper` to apply the result.

For the first submission, describe how to test on one monitor and multiple monitors, how Save differs from Apply, and that Apply temporarily changes the review machine's wallpaper.

## Version policy

Waller uses a four-component MSIX version:

```text
major.minor.build.revision
```

Rules:

- every component must be numeric and between 0 and 65535;
- no SemVer suffix belongs in `Identity.Version`;
- package name and publisher must remain stable across updates;
- version updates must not change package family identity;
- presets/settings must survive upgrades;
- rendered wallpaper files must remain readable by the Windows shell.

Use the existing version script:

```powershell
powershell -ExecutionPolicy Bypass -File .\native\scripts\SetPackageVersion.ps1 -Version 1.0.1.0
```

## Required lifecycle qualification

| Scenario | Expected result |
| --- | --- |
| Clean install | Package installs without developer tooling on the machine |
| First launch | Waller opens from Start with package identity |
| Single monitor | Session and Apply work correctly |
| Multiple monitors | Each monitor can use an independent source and placement |
| Local image | Preview/render/apply use the selected file without modifying it |
| Solid color | Render and Apply succeed |
| Placement modes | Cover, Contain, Stretch, Center, and Tile behave correctly |
| Save versus Apply | Save persists a preset; Apply changes Windows; actions remain independent |
| Cancellation | Apply cancellation leaves the app and wallpapers in a coherent state |
| Upgrade | Presets and settings remain intact |
| Uninstall | Package registration is removed; original user images are untouched |
| Reinstall | App starts cleanly and does not inherit corrupt registration |
| Standard user | Normal operation requires no elevation |
| Offline | All core wallpaper operations remain available |
| English/Spanish | Both languages remain usable and complete |

The existing Apply smoke changes wallpaper and restores it in `finally`. Keep that safeguard and record the pre/post wallpaper paths for Store release evidence.

## Privacy review

Confirm every release still matches [`../../PRIVACY.md`](../../PRIVACY.md):

- no upload of wallpaper images or paths;
- no account requirement;
- presets/settings remain local;
- rendered output location is documented;
- original selected images are never deleted;
- support artifacts are user-supplied and should be redacted.

## Release gates

- [ ] Product is reserved in Partner Center.
- [ ] `store-identity.json` has `reservationStatus: reserved` and exact values.
- [ ] `TestStoreReadiness.ps1 -RequireReservedIdentity` passes.
- [ ] Full native verification passes.
- [ ] Store upload is built from the submission commit.
- [ ] SHA-256, size, version, architecture, and source commit are recorded.
- [ ] Clean install, launch, single/multi-monitor, Apply, upgrade, uninstall, and reinstall are evidenced.
- [ ] Privacy URL is public and stable.
- [ ] English and Spanish listing copy is reviewed.
- [ ] At least four scrubbed screenshots are prepared.
- [ ] Age rating is complete.
- [ ] `runFullTrust` justification is entered.
- [ ] Certification notes explain wallpaper mutation clearly.
- [ ] First release uses an intentional publishing hold.

## First-submission procedure

1. Reserve the Waller product in Partner Center.
2. Copy identity values from Product identity.
3. Run `SetStoreIdentity.ps1` and review the diff.
4. Set the target package version.
5. Run `TestStoreReadiness.ps1 -RequireReservedIdentity`.
6. Run the complete native verification including packaged launch and Apply smoke on a safe test desktop.
7. Build the `.msixupload`.
8. Copy [`RELEASE-EVIDENCE-TEMPLATE.md`](RELEASE-EVIDENCE-TEMPLATE.md) and fill it with the exact artifact evidence.
9. Qualify the package on a clean VM/profile.
10. Complete all six Partner Center sections.
11. Submit with a manual publishing hold.
12. Review the certification report and final listing before publishing.

## Non-automatable steps

The repository cannot safely choose or complete these account-owner decisions:

- reserved Store identity;
- pricing and markets;
- company contact details;
- age-rating answers;
- privacy URL deployment/ownership;
- screenshot and listing upload;
- restricted-capability approval form;
- final certification submission and publishing decision.

## Official references

- [Create an app submission for an MSIX app](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/create-app-submission)
- [Upload MSIX app packages](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages)
- [Manage submission options](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/manage-submission-options)
- [App capability declarations](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)
- [Packaging MSIX apps](https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps)
- [Microsoft Store policies](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)
