import type { FitMode, MonitorInfo, WallpaperDraft } from './types';
import { normalizeFitMode, snapshotDraft } from './wallpaperSource';

export interface WallpaperSessionDraftState {
  drafts: Record<string, WallpaperDraft>;
  baseline: Record<string, WallpaperDraft>;
}

function createSnapshotRecord(
  record: Record<string, WallpaperDraft>,
): Record<string, WallpaperDraft> {
  return Object.fromEntries(
    Object.entries(record).map(([id, draft]) => [id, snapshotDraft(draft)]),
  ) as Record<string, WallpaperDraft>;
}

function mergeDraftRecordFromMonitors(
  previousRecord: Record<string, WallpaperDraft>,
  monitors: readonly MonitorInfo[],
  preserveDrafts: boolean,
): Record<string, WallpaperDraft> {
  return Object.fromEntries(
    monitors.map((monitor) => [
      monitor.id,
      preserveDrafts && previousRecord[monitor.id]
        ? snapshotDraft(previousRecord[monitor.id])
        : snapshotDraft({
            imagePath: monitor.currentWallpaper,
            fitMode: normalizeFitMode(monitor.currentFit),
          }),
    ]),
  ) as Record<string, WallpaperDraft>;
}

export function createWallpaperSessionDraftState(
  initial?: Partial<WallpaperSessionDraftState>,
): WallpaperSessionDraftState {
  return {
    drafts: createSnapshotRecord(initial?.drafts ?? {}),
    baseline: createSnapshotRecord(initial?.baseline ?? {}),
  };
}

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

/** Reconstruye el estado de borradores y baseline tras un refresh de monitores. */
export function refreshWallpaperSessionDraftState(
  previousState: WallpaperSessionDraftState,
  monitors: readonly MonitorInfo[],
  preserveDrafts: boolean,
): WallpaperSessionDraftState {
  return {
    drafts: mergeDraftRecordFromMonitors(previousState.drafts, monitors, preserveDrafts),
    baseline:
      preserveDrafts && Object.keys(previousState.baseline).length > 0
        ? mergeDraftRecordFromMonitors(previousState.baseline, monitors, true)
        : createDraftsFromMonitors(monitors),
  };
}

/** Devuelve el borrador efectivo de un monitor usando backend como fallback estable. */
export function readWallpaperDraft(
  draftState: WallpaperSessionDraftState,
  monitor: MonitorInfo,
): WallpaperDraft {
  return draftState.drafts[monitor.id] ?? snapshotDraft({
    imagePath: monitor.currentWallpaper,
    fitMode: normalizeFitMode(monitor.currentFit),
  });
}

/** Sustituye todos los borradores manteniendo el baseline actual. */
export function replaceWallpaperSessionDrafts(
  draftState: WallpaperSessionDraftState,
  drafts: Record<string, WallpaperDraft>,
): WallpaperSessionDraftState {
  return {
    ...draftState,
    drafts: createSnapshotRecord(drafts),
  };
}

/** Actualiza un borrador individual dentro del estado de Wallpaper Session. */
export function updateWallpaperSessionDraft(
  draftState: WallpaperSessionDraftState,
  monitorId: string,
  nextDraft: Partial<WallpaperDraft>,
): WallpaperSessionDraftState {
  return {
    ...draftState,
    drafts: {
      ...draftState.drafts,
      [monitorId]: snapshotDraft({
        ...draftState.drafts[monitorId],
        ...nextDraft,
      }),
    },
  };
}

/** Aplica un fit mode compartido a todos los Wallpaper Draft activos. */
export function setWallpaperSessionFitMode(
  draftState: WallpaperSessionDraftState,
  fitMode: FitMode,
): WallpaperSessionDraftState {
  const normalizedFitMode = normalizeFitMode(fitMode);
  return {
    ...draftState,
    drafts: Object.fromEntries(
      Object.entries(draftState.drafts).map(([monitorId, draft]) => [
        monitorId,
        snapshotDraft({ ...draft, fitMode: normalizedFitMode }),
      ]),
    ) as Record<string, WallpaperDraft>,
  };
}

/** Comprueba si un monitor tiene cambios pendientes respecto al baseline. */
export function isWallpaperSessionDirty(
  draftState: WallpaperSessionDraftState,
  monitorId: string,
): boolean {
  const current = snapshotDraft(draftState.drafts[monitorId]);
  const base = snapshotDraft(draftState.baseline[monitorId]);
  return current.imagePath !== base.imagePath || current.fitMode !== base.fitMode;
}

/** Cuenta cuántos monitores tienen cambios locales sin aplicar. */
export function countWallpaperSessionDirtyMonitors(
  draftState: WallpaperSessionDraftState,
  monitors: readonly MonitorInfo[],
): number {
  return monitors.reduce(
    (count, monitor) => count + Number(isWallpaperSessionDirty(draftState, monitor.id)),
    0,
  );
}

/** Comprueba si un monitor tiene cambios pendientes respecto al baseline. */
export function isMonitorDirty(
  monitorId: string,
  drafts: Record<string, WallpaperDraft>,
  baseline: Record<string, WallpaperDraft>,
): boolean {
  return isWallpaperSessionDirty(
    createWallpaperSessionDraftState({ drafts, baseline }),
    monitorId,
  );
}

/** Cuenta cuántos monitores tienen cambios locales sin aplicar. */
export function countDirtyMonitors(
  monitors: readonly MonitorInfo[],
  drafts: Record<string, WallpaperDraft>,
  baseline: Record<string, WallpaperDraft>,
): number {
  return countWallpaperSessionDirtyMonitors(
    createWallpaperSessionDraftState({ drafts, baseline }),
    monitors,
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
  const draftState = createWallpaperSessionDraftState({ drafts });

  return monitors.flatMap((monitor) => {
    const draft = readWallpaperDraft(draftState, monitor);
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

/** Construye el payload de apply-all usando el estado agrupado de borradores. */
export function buildWallpaperSessionApplyConfiguration(
  draftState: WallpaperSessionDraftState,
  monitors: readonly MonitorInfo[],
): Array<{ monitorId: string; imagePath: string; fitMode: FitMode }> {
  return buildApplyConfiguration(monitors, draftState.drafts);
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

/** Ajusta el baseline agrupado tras aplicar un solo Wallpaper Draft. */
export function markWallpaperSessionMonitorApplied(
  draftState: WallpaperSessionDraftState,
  monitorId: string,
  appliedDraft: WallpaperDraft,
): WallpaperSessionDraftState {
  return {
    ...draftState,
    baseline: updateBaselineAfterSingleApply(draftState.baseline, monitorId, appliedDraft),
  };
}

/** Marca como aplicado el conjunto completo de Wallpaper Draft activos. */
export function markWallpaperSessionApplied(
  draftState: WallpaperSessionDraftState,
): WallpaperSessionDraftState {
  return {
    ...draftState,
    baseline: createSnapshotRecord(draftState.drafts),
  };
}
