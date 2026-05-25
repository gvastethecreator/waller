import { formatError, normalizeErrorPayload } from './appErrors';
import {
  composeProfileSave,
  createDraftsFromProfile,
} from './profileComposition';
import {
  createPreviewRegistry,
  type PreviewRegistry,
} from './previewRegistry';
import type {
  AppErrorPayload,
  FitMode,
  LogLevel,
  MonitorInfo,
  Profile,
  ProfileMonitor,
  WallpaperDraft,
  WallpaperSource,
  WallpaperSourceType,
} from './types';
import {
  DEFAULT_FIT_MODE,
  DEFAULT_SOLID_COLOR,
  NONE_MARKER,
  makeSolidMarker,
  normalizeFitMode,
  parseWallpaperSource,
  snapshotDraft,
} from './wallpaperSource';
import {
  buildWallpaperSessionApplyConfiguration,
  countWallpaperSessionDirtyMonitors,
  createWallpaperSessionDraftState,
  isWallpaperSessionDirty,
  markWallpaperSessionApplied,
  markWallpaperSessionMonitorApplied,
  readWallpaperDraft,
  refreshWallpaperSessionDraftState,
  replaceWallpaperSessionDrafts,
  setWallpaperSessionFitMode,
  sortMonitors,
  type WallpaperSessionDraftState,
  updateWallpaperSessionDraft,
} from './wallpaperSessionState';

const IDENTIFY_FALLBACK_DELAY_MS = 700;

export interface WallpaperSessionRuntime {
  fetchMonitors(): Promise<MonitorInfo[]>;
  listProfiles(): Promise<string[]>;
  loadProfile(name: string): Promise<Profile>;
  saveProfile(name: string, monitors: ProfileMonitor[]): Promise<void>;
  deleteProfile(name: string): Promise<void>;
  pickImagePath(): Promise<string | null>;
  getImageDataUrl(imagePath: string): Promise<string>;
  applyWallpaper(monitorId: string, imagePath: string, fitMode: FitMode): Promise<void>;
  applyConfiguration(
    configs: Array<{ monitorId: string; imagePath: string; fitMode: FitMode }>,
  ): Promise<void>;
  identifyMonitors(): Promise<void>;
  saveEditedWallpaper(monitorId: string, dataUrl: string): Promise<string>;
  log?(scope: string, message: string, level?: LogLevel): Promise<void>;
}

export type WallpaperSessionStatus = 'idle' | 'loading' | 'refreshing' | 'ready';

export type WallpaperSessionPreviewState =
  | { kind: 'not-applicable' }
  | { kind: 'loading'; imagePath: string }
  | { kind: 'ready'; imagePath: string; dataUrl: string }
  | { kind: 'error'; imagePath: string; error: AppErrorPayload };

export interface WallpaperSessionMonitorView {
  monitor: MonitorInfo;
  draft: WallpaperDraft;
  source: WallpaperSource;
  preview: WallpaperSessionPreviewState;
  dirty: boolean;
  canEdit: boolean;
}

export interface WallpaperSessionEditorView {
  open: boolean;
  monitor: MonitorInfo | null;
  sourceImagePath: string;
  fitMode: FitMode;
  isSaving: boolean;
  error: AppErrorPayload | null;
}

export interface WallpaperSessionSnapshot {
  status: WallpaperSessionStatus;
  monitors: WallpaperSessionMonitorView[];
  profiles: string[];
  dirtyCount: number;
  diagnosticMode: boolean;
  editor: WallpaperSessionEditorView;
  identifyOverlay: {
    highlightedMonitorId: string | null;
    isRunning: boolean;
  };
}

export type WallpaperSessionCommand =
  | { type: 'refresh'; preserveDrafts?: boolean }
  | { type: 'set-source-type'; monitorId: string; sourceType: WallpaperSourceType }
  | { type: 'choose-monitor-image'; monitorId: string }
  | { type: 'set-solid-color'; monitorId: string; color: string }
  | { type: 'set-fit-mode'; fitMode: FitMode }
  | { type: 'clear-monitor'; monitorId: string }
  | { type: 'apply-monitor'; monitorId: string }
  | { type: 'apply-all' }
  | { type: 'load-profile'; name: string }
  | { type: 'save-profile'; name: string }
  | { type: 'delete-profile'; name: string }
  | { type: 'open-editor'; monitorId: string }
  | { type: 'pick-editor-image' }
  | { type: 'save-editor'; dataUrl: string }
  | { type: 'close-editor' }
  | { type: 'resolve-preview'; imagePath: string }
  | { type: 'identify' };

export interface WallpaperSessionStore {
  read(): WallpaperSessionSnapshot;
  subscribe(listener: () => void): () => void;
  send(command: { type: 'choose-monitor-image'; monitorId: string }): Promise<string | null>;
  send(command: { type: 'pick-editor-image' }): Promise<string | null>;
  send(command: { type: 'resolve-preview'; imagePath: string }): Promise<string>;
  send(
    command: Exclude<
      WallpaperSessionCommand,
      | { type: 'choose-monitor-image'; monitorId: string }
      | { type: 'pick-editor-image' }
      | { type: 'resolve-preview'; imagePath: string }
    >,
  ): Promise<void>;
  dispose(): void;
}

interface WallpaperSessionStoreOptions {
  runtime: WallpaperSessionRuntime;
  identifyFallbackDelayMs?: number;
}

interface InternalEditorState {
  open: boolean;
  monitorId: string | null;
  sourceImagePath: string;
  isSaving: boolean;
  error: AppErrorPayload | null;
}

interface InternalState {
  status: WallpaperSessionStatus;
  monitors: MonitorInfo[];
  draftState: WallpaperSessionDraftState;
  profiles: string[];
  editor: InternalEditorState;
  identifyOverlay: {
    highlightedMonitorId: string | null;
    isRunning: boolean;
  };
}

function createSessionError(
  code: string,
  message: string,
  details?: string,
): AppErrorPayload {
  return details ? { code, message, details } : { code, message };
}

function createClosedEditorState(): InternalEditorState {
  return {
    open: false,
    monitorId: null,
    sourceImagePath: '',
    isSaving: false,
    error: null,
  };
}

function createEmptyState(): InternalState {
  return {
    status: 'idle',
    monitors: [],
    draftState: createWallpaperSessionDraftState(),
    profiles: [],
    editor: createClosedEditorState(),
    identifyOverlay: {
      highlightedMonitorId: null,
      isRunning: false,
    },
  };
}

function hasFallbackMonitorIds(monitors: readonly MonitorInfo[]): boolean {
  return monitors.some((monitor) => monitor.id.startsWith('GDI_MONITOR_'));
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, milliseconds);
  });
}

function buildMonitorViews(
  state: InternalState,
  previewRegistry: PreviewRegistry,
): WallpaperSessionMonitorView[] {
  const sortedMonitors = sortMonitors(state.monitors);

  return sortedMonitors.map((monitor) => {
    const draft = readWallpaperDraft(state.draftState, monitor);
    const source = parseWallpaperSource(draft.imagePath);

    let preview: WallpaperSessionPreviewState = { kind: 'not-applicable' };
    if (source.type === 'image' && source.imagePath) {
      const previewEntry = previewRegistry.read(source.imagePath);
      if (previewEntry.kind === 'ready') {
        preview = {
          kind: 'ready',
          imagePath: source.imagePath,
          dataUrl: previewEntry.dataUrl,
        };
      } else if (previewEntry.kind === 'loading') {
        preview = {
          kind: 'loading',
          imagePath: source.imagePath,
        };
      } else if (previewEntry.kind === 'error') {
        preview = {
          kind: 'error',
          imagePath: source.imagePath,
          error: previewEntry.error,
        };
      }
    }

    return {
      monitor,
      draft,
      source,
      preview,
      dirty: isWallpaperSessionDirty(state.draftState, monitor.id),
      canEdit: !monitor.id.startsWith('GDI_MONITOR_'),
    };
  });
}

function buildSnapshot(
  state: InternalState,
  previewRegistry: PreviewRegistry,
): WallpaperSessionSnapshot {
  const monitors = buildMonitorViews(state, previewRegistry);
  const sortedMonitors = monitors.map((entry) => entry.monitor);
  const editorMonitor = state.editor.monitorId
    ? sortedMonitors.find((monitor) => monitor.id === state.editor.monitorId) ?? null
    : null;
  const editorDraft = editorMonitor
    ? readWallpaperDraft(state.draftState, editorMonitor)
    : null;

  return {
    status: state.status,
    monitors,
    profiles: [...state.profiles],
    dirtyCount: countWallpaperSessionDirtyMonitors(state.draftState, sortedMonitors),
    diagnosticMode: hasFallbackMonitorIds(sortedMonitors),
    editor: {
      open: state.editor.open && editorMonitor !== null,
      monitor: editorMonitor,
      sourceImagePath: state.editor.sourceImagePath,
      fitMode: editorDraft ? normalizeFitMode(editorDraft.fitMode) : DEFAULT_FIT_MODE,
      isSaving: state.editor.isSaving,
      error: state.editor.error,
    },
    identifyOverlay: {
      highlightedMonitorId: state.identifyOverlay.highlightedMonitorId,
      isRunning: state.identifyOverlay.isRunning,
    },
  };
}

export function createWallpaperSessionStore({
  runtime,
  identifyFallbackDelayMs = IDENTIFY_FALLBACK_DELAY_MS,
}: WallpaperSessionStoreOptions): WallpaperSessionStore {
  let state = createEmptyState();
  let snapshot: WallpaperSessionSnapshot;
  let previewRegistry: PreviewRegistry;
  let disposed = false;
  let commandQueue: Promise<void> = Promise.resolve();
  const listeners = new Set<() => void>();

  const notify = () => {
    if (disposed) {
      return;
    }

    for (const listener of listeners) {
      listener();
    }
  };

  const rebuildSnapshot = () => {
    snapshot = buildSnapshot(state, previewRegistry);
  };

  previewRegistry = createPreviewRegistry({
    onChange: () => {
      rebuildSnapshot();
      notify();
    },
  });
  rebuildSnapshot();

  const setState = (updater: (previous: InternalState) => InternalState) => {
    state = updater(state);
    rebuildSnapshot();
    notify();
  };

  const log = (scope: string, message: string, level: LogLevel = 'info') => {
    if (!runtime.log) {
      return;
    }

    void runtime.log(scope, message, level).catch(() => undefined);
  };

  const ensureMonitor = (monitorId: string): MonitorInfo => {
    const monitor = state.monitors.find((candidate) => candidate.id === monitorId);
    if (!monitor) {
      throw createSessionError('monitor_not_found', `Monitor ${monitorId} not found`);
    }
    return monitor;
  };

  const resolvePreviewDataUrl = async (imagePath: string): Promise<string> => {
    if (!imagePath) {
      throw createSessionError('preview_source_required', 'Image path cannot be empty');
    }

    return previewRegistry.resolve(imagePath, async () => {
      try {
        const dataUrl = await runtime.getImageDataUrl(imagePath);
        log('preview', `preview success for ${imagePath}`, 'debug');
        return dataUrl;
      } catch (error) {
        const normalized = normalizeErrorPayload(error);
        log('preview', `preview error for ${imagePath}: ${formatError(normalized)}`, 'warn');
        throw normalized;
      }
    });
  };

  const warmPreview = (imagePath: string) => {
    if (!imagePath) {
      return;
    }

    void resolvePreviewDataUrl(imagePath).catch(() => undefined);
  };

  const warmVisiblePreviews = () => {
    const imagePaths = new Set<string>();
    for (const monitor of sortMonitors(state.monitors)) {
      const draft = readWallpaperDraft(state.draftState, monitor);
      const source = parseWallpaperSource(draft.imagePath);
      if (source.type === 'image' && source.imagePath) {
        imagePaths.add(source.imagePath);
      }
    }

    if (state.editor.sourceImagePath) {
      imagePaths.add(state.editor.sourceImagePath);
    }

    for (const imagePath of imagePaths) {
      warmPreview(imagePath);
    }
  };

  const updateDraft = (monitorId: string, nextDraft: Partial<WallpaperDraft>) => {
    setState((previous) => ({
      ...previous,
      draftState: updateWallpaperSessionDraft(previous.draftState, monitorId, nextDraft),
    }));
  };

  const chooseImageForMonitor = async (monitorId: string): Promise<string | null> => {
    ensureMonitor(monitorId);
    const path = await runtime.pickImagePath();
    if (!path) {
      return null;
    }

    log('browse', `selected image for monitor ${monitorId}: ${path}`, 'info');
    setState((previous) => ({
      ...previous,
      draftState: updateWallpaperSessionDraft(previous.draftState, monitorId, {
        imagePath: path,
      }),
    }));
    previewRegistry.clear(path);
    warmPreview(path);
    return path;
  };

  const queue = <T>(operation: () => Promise<T>): Promise<T> => {
    const next = commandQueue.then(operation, operation);
    commandQueue = next.then(
      () => undefined,
      () => undefined,
    );
    return next;
  };

  const refresh = async (preserveDrafts = false): Promise<void> => {
    const nextStatus: WallpaperSessionStatus = state.monitors.length ? 'refreshing' : 'loading';
    setState((previous) => ({ ...previous, status: nextStatus }));

    try {
      log('monitors', 'get_monitors invoke start', 'debug');
      const [monitors, profiles] = await Promise.all([
        runtime.fetchMonitors(),
        runtime.listProfiles(),
      ]);
      const nextMonitors = sortMonitors(monitors);

      setState((previous) => {
        const nextEditorMonitorId = previous.editor.monitorId;
        const editorStillAvailable = nextEditorMonitorId
          ? nextMonitors.some((monitor) => monitor.id === nextEditorMonitorId)
          : false;

        return {
          ...previous,
          status: 'ready',
          monitors: nextMonitors,
          draftState: refreshWallpaperSessionDraftState(
            previous.draftState,
            nextMonitors,
            preserveDrafts,
          ),
          profiles,
          editor: editorStillAvailable ? previous.editor : createClosedEditorState(),
        };
      });

      log('monitors', `get_monitors ok: ${nextMonitors.length} monitor(s)`, 'info');
      warmVisiblePreviews();
    } catch (error) {
      const normalized = normalizeErrorPayload(error);
      setState((previous) => ({
        ...previous,
        status: previous.monitors.length ? 'ready' : 'idle',
      }));
      log('monitors', `get_monitors error: ${formatError(normalized)}`, 'error');
      throw normalized;
    }
  };

  async function send(
    command: { type: 'choose-monitor-image'; monitorId: string },
  ): Promise<string | null>;
  async function send(command: { type: 'pick-editor-image' }): Promise<string | null>;
  async function send(command: { type: 'resolve-preview'; imagePath: string }): Promise<string>;
  async function send(
    command: Exclude<
      WallpaperSessionCommand,
      | { type: 'choose-monitor-image'; monitorId: string }
      | { type: 'pick-editor-image' }
      | { type: 'resolve-preview'; imagePath: string }
    >,
  ): Promise<void>;
  async function send(command: WallpaperSessionCommand): Promise<string | null | void> {
    switch (command.type) {
      case 'resolve-preview':
        return resolvePreviewDataUrl(command.imagePath);

      case 'refresh':
        return queue(async () => {
          await refresh(command.preserveDrafts ?? false);
        });

      case 'choose-monitor-image':
        return queue(async () => chooseImageForMonitor(command.monitorId));

      case 'set-source-type':
        return queue(async () => {
          const monitor = ensureMonitor(command.monitorId);
          const currentDraft = readWallpaperDraft(state.draftState, monitor);
          const currentSource = parseWallpaperSource(currentDraft.imagePath);

          if (command.sourceType === 'image') {
            if (currentSource.type === 'image' && currentSource.imagePath) {
              return;
            }
            await chooseImageForMonitor(command.monitorId);
            return;
          }

          if (command.sourceType === 'solid') {
            updateDraft(command.monitorId, {
              imagePath: makeSolidMarker(
                currentSource.type === 'solid' ? currentSource.color : DEFAULT_SOLID_COLOR,
              ),
            });
            return;
          }

          updateDraft(command.monitorId, { imagePath: NONE_MARKER });
        });

      case 'set-solid-color':
        return queue(async () => {
          ensureMonitor(command.monitorId);
          updateDraft(command.monitorId, {
            imagePath: makeSolidMarker(command.color),
          });
        });

      case 'set-fit-mode':
        return queue(async () => {
          setState((previous) => ({
            ...previous,
            draftState: setWallpaperSessionFitMode(previous.draftState, command.fitMode),
          }));
        });

      case 'clear-monitor':
        return queue(async () => {
          ensureMonitor(command.monitorId);
          updateDraft(command.monitorId, { imagePath: NONE_MARKER });
        });

      case 'apply-monitor':
        return queue(async () => {
          const monitor = ensureMonitor(command.monitorId);
          const draft = snapshotDraft(readWallpaperDraft(state.draftState, monitor));
          log('apply', `apply_wallpaper start: ${command.monitorId}`, 'info');
          await runtime.applyWallpaper(command.monitorId, draft.imagePath, draft.fitMode);
          setState((previous) => ({
            ...previous,
            draftState: markWallpaperSessionMonitorApplied(
              previous.draftState,
              command.monitorId,
              draft,
            ),
          }));
          log('apply', `apply_wallpaper success: ${command.monitorId}`, 'info');
        });

      case 'apply-all':
        return queue(async () => {
          const sortedMonitors = sortMonitors(state.monitors);
          if (hasFallbackMonitorIds(sortedMonitors)) {
            throw createSessionError(
              'diagnostic_mode',
              'Apply all is disabled while diagnostic monitors are active',
            );
          }

          const configs = buildWallpaperSessionApplyConfiguration(
            state.draftState,
            sortedMonitors,
          );
          if (!configs.length) {
            throw createSessionError('no_wallpapers', 'No wallpapers configured to apply');
          }

          log('apply', `apply_configuration start: ${configs.length} config(s)`, 'info');
          await runtime.applyConfiguration(configs);
          setState((previous) => ({
            ...previous,
            draftState: markWallpaperSessionApplied(previous.draftState),
          }));
          log('apply', 'apply_configuration success', 'info');
        });

      case 'load-profile':
        return queue(async () => {
          const profile = await runtime.loadProfile(command.name);
          const sortedMonitors = sortMonitors(state.monitors);
          const nextDrafts = createDraftsFromProfile(profile, sortedMonitors);

          setState((previous) => ({
            ...previous,
            draftState: replaceWallpaperSessionDrafts(previous.draftState, nextDrafts),
          }));
          warmVisiblePreviews();
        });

      case 'save-profile':
        return queue(async () => {
          const profileSave = composeProfileSave(command.name, state.draftState.drafts);

          await runtime.saveProfile(profileSave.name, profileSave.monitors);
          const nextProfiles = await runtime.listProfiles();
          setState((previous) => ({
            ...previous,
            profiles: nextProfiles,
          }));
        });

      case 'delete-profile':
        return queue(async () => {
          await runtime.deleteProfile(command.name);
          const nextProfiles = await runtime.listProfiles();
          setState((previous) => ({
            ...previous,
            profiles: nextProfiles,
          }));
        });

      case 'open-editor':
        return queue(async () => {
          const monitor = ensureMonitor(command.monitorId);
          if (monitor.id.startsWith('GDI_MONITOR_')) {
            throw createSessionError(
              'editor_not_available',
              'Editor is unavailable for diagnostic monitors',
            );
          }

          const draft = readWallpaperDraft(state.draftState, monitor);
          const source = parseWallpaperSource(draft.imagePath);
          let sourceImagePath = source.type === 'image' ? source.imagePath : '';

          if (!sourceImagePath) {
            sourceImagePath = (await runtime.pickImagePath()) ?? '';
          }

          if (!sourceImagePath) {
            return;
          }

          setState((previous) => ({
            ...previous,
            editor: {
              open: true,
              monitorId: command.monitorId,
              sourceImagePath,
              isSaving: false,
              error: null,
            },
          }));
          warmPreview(sourceImagePath);
        });

      case 'pick-editor-image':
        return queue(async () => {
          if (!state.editor.open || !state.editor.monitorId) {
            throw createSessionError('editor_not_open', 'Editor is not open');
          }

          const nextPath = await runtime.pickImagePath();
          if (!nextPath) {
            return null;
          }

          setState((previous) => ({
            ...previous,
            editor: {
              ...previous.editor,
              sourceImagePath: nextPath,
              error: null,
            },
          }));
          previewRegistry.clear(nextPath);
          warmPreview(nextPath);
          return nextPath;
        });

      case 'save-editor':
        return queue(async () => {
          if (!state.editor.open || !state.editor.monitorId) {
            throw createSessionError('editor_not_open', 'Editor is not open');
          }

          const monitorId = state.editor.monitorId;
          const monitor = ensureMonitor(monitorId);
          const fitMode = normalizeFitMode(readWallpaperDraft(state.draftState, monitor).fitMode);
          setState((previous) => ({
            ...previous,
            editor: {
              ...previous.editor,
              isSaving: true,
              error: null,
            },
          }));

          try {
            log('editor', `save start for ${monitorId}`, 'info');
            const savedPath = await runtime.saveEditedWallpaper(monitorId, command.dataUrl);
            await runtime.applyWallpaper(monitorId, savedPath, fitMode);

            const nextDraft = snapshotDraft({ imagePath: savedPath, fitMode });
            setState((previous) => ({
              ...previous,
              draftState: markWallpaperSessionMonitorApplied(
                updateWallpaperSessionDraft(previous.draftState, monitorId, nextDraft),
                monitorId,
                nextDraft,
              ),
              editor: createClosedEditorState(),
            }));
            previewRegistry.remember(savedPath, command.dataUrl);
            log('editor', `save success for ${monitorId}`, 'info');
          } catch (error) {
            const normalized = normalizeErrorPayload(error);
            setState((previous) => ({
              ...previous,
              editor: {
                ...previous.editor,
                isSaving: false,
                error: normalized,
              },
            }));
            log('editor', `save error for ${monitorId}: ${formatError(normalized)}`, 'error');
            throw normalized;
          }
        });

      case 'close-editor':
        return queue(async () => {
          setState((previous) => ({
            ...previous,
            editor: createClosedEditorState(),
          }));
        });

      case 'identify':
        return queue(async () => {
          const sortedMonitors = sortMonitors(state.monitors);
          if (!sortedMonitors.length) {
            throw createSessionError('no_monitors', 'No monitors available to identify');
          }

          try {
            await runtime.identifyMonitors();
            setState((previous) => ({
              ...previous,
              identifyOverlay: {
                highlightedMonitorId: null,
                isRunning: false,
              },
            }));
            return;
          } catch {
            setState((previous) => ({
              ...previous,
              identifyOverlay: {
                ...previous.identifyOverlay,
                isRunning: true,
              },
            }));
          }

          try {
            for (const monitor of sortedMonitors) {
              setState((previous) => ({
                ...previous,
                identifyOverlay: {
                  highlightedMonitorId: monitor.id,
                  isRunning: true,
                },
              }));
              await delay(identifyFallbackDelayMs);
            }
          } finally {
            setState((previous) => ({
              ...previous,
              identifyOverlay: {
                highlightedMonitorId: null,
                isRunning: false,
              },
            }));
          }
        });

      default:
        return undefined;
    }
  }

  return {
    read: () => snapshot,
    subscribe(listener) {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    send,
    dispose() {
      disposed = true;
      listeners.clear();
      previewRegistry.dispose();
    },
  };
}
