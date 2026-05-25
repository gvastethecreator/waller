import type { FitMode, MonitorInfo, WallpaperDraft } from './types';
import { normalizeFitMode, snapshotDraft } from './wallpaperSource';

/** Ordena monitores por índice visual para mantener una UI estable. */
export function sortMonitors(monitors: readonly MonitorInfo[]): MonitorInfo[] {
  return [...monitors].sort((left, right) => left.displayIndex - right.displayIndex);
}

/** Crea el mapa inicial de borradores desde el backend. */
export function createDraftsFromMonitors(
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
  );
}

/** Comprueba si un monitor tiene cambios pendientes respecto al baseline. */
export function isMonitorDirty(
  monitorId: string,
  drafts: Record<string, WallpaperDraft>,
  baseline: Record<string, WallpaperDraft>,
): boolean {
  const current = snapshotDraft(drafts[monitorId]);
  const base = snapshotDraft(baseline[monitorId]);
  return current.imagePath !== base.imagePath || current.fitMode !== base.fitMode;
}

/** Cuenta cuántos monitores tienen cambios locales sin aplicar. */
export function countDirtyMonitors(
  monitors: readonly MonitorInfo[],
  drafts: Record<string, WallpaperDraft>,
  baseline: Record<string, WallpaperDraft>,
): number {
  return monitors.reduce(
    (count, monitor) => count + Number(isMonitorDirty(monitor.id, drafts, baseline)),
    0,
  );
}

/** Elimina una clave de un diccionario inmutable. */
export function removeKey<T>(record: Record<string, T>, key: string): Record<string, T> {
  const { [key]: _ignored, ...rest } = record;
  return rest;
}

/** Construye el payload de configuración que consume el backend para aplicar todos los monitores. */
export function buildApplyConfiguration(
  monitors: readonly MonitorInfo[],
  drafts: Record<string, WallpaperDraft>,
): Array<{ monitorId: string; imagePath: string; fitMode: FitMode }> {
  return monitors.flatMap((monitor) => {
    const draft = drafts[monitor.id];
    if (!draft?.imagePath) {
      return [];
    }

    return [
      {
        monitorId: monitor.id,
        imagePath: draft.imagePath,
        fitMode: normalizeFitMode(draft.fitMode),
      },
    ];
  });
}

/** Ajusta el baseline cuando Windows cambia el fit global tras aplicar un solo monitor. */
export function updateBaselineAfterSingleApply(
  baseline: Record<string, WallpaperDraft>,
  monitorId: string,
  appliedDraft: WallpaperDraft,
): Record<string, WallpaperDraft> {
  const next = Object.fromEntries(
    Object.entries(baseline).map(([id, draft]) => [
      id,
      snapshotDraft({ ...draft, fitMode: appliedDraft.fitMode }),
    ]),
  ) as Record<string, WallpaperDraft>;

  next[monitorId] = snapshotDraft(appliedDraft);
  return next;
}
