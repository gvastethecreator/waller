import { useCallback, useEffect, useMemo, useSyncExternalStore } from 'react';
import {
  createWallpaperSessionStore,
  type WallpaperSessionSnapshot,
  type WallpaperSessionRuntime,
} from '../lib/wallpaperSession';
import type { FitMode, WallpaperSourceType } from '../lib/types';

interface UseWallpaperSessionOptions {
  runtime: WallpaperSessionRuntime;
  identifyFallbackDelayMs?: number;
}

export interface WallpaperSessionFlowActions {
  refresh(preserveDrafts?: boolean): Promise<void>;
  applyAll(): Promise<void>;
  identify(): Promise<void>;
}

export interface WallpaperDraftActions {
  chooseImage(monitorId: string): Promise<string | null>;
  setSourceType(monitorId: string, sourceType: WallpaperSourceType): Promise<void>;
  setSolidColor(monitorId: string, color: string): Promise<void>;
  setFitMode(fitMode: FitMode): Promise<void>;
  clear(monitorId: string): Promise<void>;
  apply(monitorId: string): Promise<void>;
}

export interface WallpaperProfileActions {
  load(name: string): Promise<void>;
  save(name: string): Promise<void>;
  delete(name: string): Promise<void>;
}

export interface WallpaperEditorActions {
  open(monitorId: string): Promise<void>;
  pickImage(): Promise<string | null>;
  save(dataUrl: string): Promise<void>;
  close(): Promise<void>;
}

export interface WallpaperPreviewActions {
  resolveDataUrl(imagePath: string): Promise<string>;
}

export interface UseWallpaperSessionResult {
  snapshot: WallpaperSessionSnapshot;
  session: WallpaperSessionFlowActions;
  monitorDrafts: WallpaperDraftActions;
  profiles: WallpaperProfileActions;
  editor: WallpaperEditorActions;
  previews: WallpaperPreviewActions;
  refresh: WallpaperSessionFlowActions['refresh'];
  chooseMonitorImage: WallpaperDraftActions['chooseImage'];
  setSourceType: WallpaperDraftActions['setSourceType'];
  setSolidColor: WallpaperDraftActions['setSolidColor'];
  setFitMode: WallpaperDraftActions['setFitMode'];
  clearMonitor: WallpaperDraftActions['clear'];
  applyMonitor: WallpaperDraftActions['apply'];
  applyAll: WallpaperSessionFlowActions['applyAll'];
  loadProfile: WallpaperProfileActions['load'];
  saveProfile: WallpaperProfileActions['save'];
  deleteProfile: WallpaperProfileActions['delete'];
  openEditor: WallpaperEditorActions['open'];
  pickEditorImage: WallpaperEditorActions['pickImage'];
  saveEditor: WallpaperEditorActions['save'];
  closeEditor: WallpaperEditorActions['close'];
  identify: WallpaperSessionFlowActions['identify'];
  resolvePreviewDataUrl: WallpaperPreviewActions['resolveDataUrl'];
}

export function useWallpaperSession({
  runtime,
  identifyFallbackDelayMs,
}: UseWallpaperSessionOptions): UseWallpaperSessionResult {
  const store = useMemo(
    () => {
      const options = identifyFallbackDelayMs === undefined
        ? { runtime }
        : { runtime, identifyFallbackDelayMs };
      return createWallpaperSessionStore(options);
    },
    [identifyFallbackDelayMs, runtime],
  );

  useEffect(() => () => store.dispose(), [store]);

  const snapshot = useSyncExternalStore(store.subscribe, store.read, store.read);

  const refresh = useCallback(
    (preserveDrafts = false) => store.send({ type: 'refresh', preserveDrafts }),
    [store],
  );
  const chooseMonitorImage = useCallback(
    (monitorId: string) => store.send({ type: 'choose-monitor-image', monitorId }),
    [store],
  );
  const setSourceType = useCallback(
    (monitorId: string, sourceType: WallpaperSourceType) =>
      store.send({ type: 'set-source-type', monitorId, sourceType }),
    [store],
  );
  const setSolidColor = useCallback(
    (monitorId: string, color: string) =>
      store.send({ type: 'set-solid-color', monitorId, color }),
    [store],
  );
  const setFitMode = useCallback(
    (fitMode: FitMode) => store.send({ type: 'set-fit-mode', fitMode }),
    [store],
  );
  const clearMonitor = useCallback(
    (monitorId: string) => store.send({ type: 'clear-monitor', monitorId }),
    [store],
  );
  const applyMonitor = useCallback(
    (monitorId: string) => store.send({ type: 'apply-monitor', monitorId }),
    [store],
  );
  const applyAll = useCallback(() => store.send({ type: 'apply-all' }), [store]);
  const loadProfile = useCallback(
    (name: string) => store.send({ type: 'load-profile', name }),
    [store],
  );
  const saveProfile = useCallback(
    (name: string) => store.send({ type: 'save-profile', name }),
    [store],
  );
  const deleteProfile = useCallback(
    (name: string) => store.send({ type: 'delete-profile', name }),
    [store],
  );
  const openEditor = useCallback(
    (monitorId: string) => store.send({ type: 'open-editor', monitorId }),
    [store],
  );
  const pickEditorImage = useCallback(
    () => store.send({ type: 'pick-editor-image' }),
    [store],
  );
  const saveEditor = useCallback(
    (dataUrl: string) => store.send({ type: 'save-editor', dataUrl }),
    [store],
  );
  const closeEditor = useCallback(
    () => store.send({ type: 'close-editor' }),
    [store],
  );
  const identify = useCallback(() => store.send({ type: 'identify' }), [store]);
  const resolvePreviewDataUrl = useCallback(
    (imagePath: string) => store.send({ type: 'resolve-preview', imagePath }),
    [store],
  );

  const session = useMemo<WallpaperSessionFlowActions>(
    () => ({
      refresh,
      applyAll,
      identify,
    }),
    [applyAll, identify, refresh],
  );

  const monitorDrafts = useMemo<WallpaperDraftActions>(
    () => ({
      chooseImage: chooseMonitorImage,
      setSourceType,
      setSolidColor,
      setFitMode,
      clear: clearMonitor,
      apply: applyMonitor,
    }),
    [applyMonitor, chooseMonitorImage, clearMonitor, setFitMode, setSolidColor, setSourceType],
  );

  const profiles = useMemo<WallpaperProfileActions>(
    () => ({
      load: loadProfile,
      save: saveProfile,
      delete: deleteProfile,
    }),
    [deleteProfile, loadProfile, saveProfile],
  );

  const editor = useMemo<WallpaperEditorActions>(
    () => ({
      open: openEditor,
      pickImage: pickEditorImage,
      save: saveEditor,
      close: closeEditor,
    }),
    [closeEditor, openEditor, pickEditorImage, saveEditor],
  );

  const previews = useMemo<WallpaperPreviewActions>(
    () => ({
      resolveDataUrl: resolvePreviewDataUrl,
    }),
    [resolvePreviewDataUrl],
  );

  return {
    snapshot,
    session,
    monitorDrafts,
    profiles,
    editor,
    previews,
    refresh,
    chooseMonitorImage,
    setSourceType,
    setSolidColor,
    setFitMode,
    clearMonitor,
    applyMonitor,
    applyAll,
    loadProfile,
    saveProfile,
    deleteProfile,
    openEditor,
    pickEditorImage,
    saveEditor,
    closeEditor,
    identify,
    resolvePreviewDataUrl,
  };
}