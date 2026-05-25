import { useCallback, useEffect, useMemo, useSyncExternalStore } from 'react';
import {
  createWallpaperSessionStore,
  type WallpaperSessionRuntime,
} from '../lib/wallpaperSession';
import type { FitMode, WallpaperSourceType } from '../lib/types';

interface UseWallpaperSessionOptions {
  runtime: WallpaperSessionRuntime;
  identifyFallbackDelayMs?: number;
}

export function useWallpaperSession({
  runtime,
  identifyFallbackDelayMs,
}: UseWallpaperSessionOptions) {
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

  return {
    snapshot,
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