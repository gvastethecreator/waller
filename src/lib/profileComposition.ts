import type {
  AppErrorPayload,
  MonitorInfo,
  Profile,
  ProfileMonitor,
  WallpaperDraft,
} from './types';
import {
  isSupportedFitMode,
  normalizeFitMode,
  snapshotDraft,
} from './wallpaperSource';

const MAX_PROFILE_NAME_LENGTH = 80;
const MAX_PROFILE_MONITORS = 32;
const MAX_PROFILE_IMAGE_PATH_LENGTH = 4096;

export interface PreparedProfileSave {
  name: string;
  monitors: ProfileMonitor[];
}

function createProfileCompositionError(
  code: string,
  message: string,
  details?: string,
): AppErrorPayload {
  return details ? { code, message, details } : { code, message };
}

function throwProfileCompositionError(
  code: string,
  message: string,
  details?: string,
): never {
  throw createProfileCompositionError(code, message, details);
}

function createDraftsFromMonitors(
  monitors: readonly MonitorInfo[],
): Record<string, WallpaperDraft> {
  return Object.fromEntries(
    monitors.map((monitor) => [
      monitor.id,
      snapshotDraft({
        imagePath: monitor.currentWallpaper,
        fitMode: normalizeFitMode(monitor.currentFit),
      }),
    ]),
  ) as Record<string, WallpaperDraft>;
}

export function validateProfileName(name: string): string {
  const trimmed = name.trim();
  if (!trimmed) {
    throwProfileCompositionError('profile_name_required', 'Profile name is required');
  }

  if (trimmed.length > MAX_PROFILE_NAME_LENGTH) {
    throwProfileCompositionError(
      'profile_name_too_long',
      `Profile name is too long (max ${MAX_PROFILE_NAME_LENGTH} chars)`,
    );
  }

  return trimmed;
}

export function draftsToProfileMonitors(
  drafts: Record<string, WallpaperDraft>,
): ProfileMonitor[] {
  return Object.entries(drafts).map(([monitorId, draft]) => {
    const normalizedDraft = snapshotDraft(draft);
    return {
      monitorId,
      imagePath: normalizedDraft.imagePath,
      fitMode: normalizedDraft.fitMode,
    };
  });
}

export function validateProfileMonitors(monitors: readonly ProfileMonitor[]): void {
  if (monitors.length > MAX_PROFILE_MONITORS) {
    throwProfileCompositionError(
      'profile_monitor_limit',
      `Profile contains too many monitors (max ${MAX_PROFILE_MONITORS})`,
    );
  }

  const seenMonitorIds = new Set<string>();
  for (const monitor of monitors) {
    const monitorId = monitor.monitorId.trim();
    if (!monitorId) {
      throwProfileCompositionError(
        'profile_monitor_id_required',
        'Profile contains a monitor with empty ID',
      );
    }

    if (!seenMonitorIds.add(monitorId)) {
      throwProfileCompositionError(
        'profile_monitor_duplicate',
        `Profile contains duplicate monitor ID: ${monitorId}`,
      );
    }

    if (!isSupportedFitMode(monitor.fitMode)) {
      throwProfileCompositionError(
        'profile_monitor_fit_invalid',
        `Unsupported fit mode in profile: ${monitor.fitMode}`,
        monitorId,
      );
    }

    if (monitor.imagePath.length > MAX_PROFILE_IMAGE_PATH_LENGTH) {
      throwProfileCompositionError(
        'profile_monitor_image_path_too_long',
        `Image path too long for monitor: ${monitorId}`,
      );
    }
  }
}

export function composeProfileSave(
  name: string,
  drafts: Record<string, WallpaperDraft>,
): PreparedProfileSave {
  const normalizedName = validateProfileName(name);
  const monitors = draftsToProfileMonitors(drafts);
  validateProfileMonitors(monitors);
  return {
    name: normalizedName,
    monitors,
  };
}

export function createDraftsFromProfile(
  profile: Profile,
  activeMonitors: readonly MonitorInfo[],
): Record<string, WallpaperDraft> {
  validateProfileName(profile.profileName);
  validateProfileMonitors(profile.monitors);

  const nextDrafts = createDraftsFromMonitors(activeMonitors);
  const activeMonitorIds = new Set(activeMonitors.map((monitor) => monitor.id));

  for (const monitor of profile.monitors) {
    if (!activeMonitorIds.has(monitor.monitorId)) {
      continue;
    }

    nextDrafts[monitor.monitorId] = snapshotDraft({
      imagePath: monitor.imagePath,
      fitMode: monitor.fitMode,
    });
  }

  return nextDrafts;
}
