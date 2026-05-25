import { describe, expect, it, vi } from 'vitest';
import type { MonitorInfo, Profile } from './types';
import {
  createWallpaperSessionStore,
  type WallpaperSessionRuntime,
} from './wallpaperSession';

const multiMonitorFixture: MonitorInfo[] = [
  {
    id: 'DISPLAY1',
    displayIndex: 1,
    name: 'Monitor 1',
    width: 1920,
    height: 1080,
    x: 0,
    y: 0,
    currentWallpaper: 'first.png',
    currentFit: 'Fill',
  },
  {
    id: 'DISPLAY2',
    displayIndex: 2,
    name: 'Monitor 2',
    width: 2560,
    height: 1440,
    x: 1920,
    y: 0,
    currentWallpaper: '__NONE__',
    currentFit: 'Fit',
  },
];

const singleMonitorFixture: MonitorInfo[] = [
  {
    id: 'DISPLAY1',
    displayIndex: 1,
    name: 'Monitor 1',
    width: 1920,
    height: 1080,
    x: 0,
    y: 0,
    currentWallpaper: '__NONE__',
    currentFit: 'Fill',
  },
];

function createRuntime(options?: {
  monitors?: MonitorInfo[];
  profile?: Profile;
  pickImagePath?: string | null;
  getImageDataUrl?: (imagePath: string) => Promise<string>;
}) {
  const profile = options?.profile ?? {
    profileName: 'Desk',
    monitors: [
      {
        monitorId: 'DISPLAY1',
        imagePath: 'profile.png',
        fitMode: 'Span',
      },
      {
        monitorId: 'MISSING',
        imagePath: 'orphan.png',
        fitMode: 'Fill',
      },
    ],
  };

  const runtime: WallpaperSessionRuntime = {
    fetchMonitors: vi.fn(async () => options?.monitors ?? multiMonitorFixture),
    listProfiles: vi.fn(async () => ['Desk']),
    loadProfile: vi.fn(async () => profile),
    saveProfile: vi.fn(async () => undefined),
    deleteProfile: vi.fn(async () => undefined),
    pickImagePath: vi.fn(async () => options?.pickImagePath ?? 'picked.png'),
    getImageDataUrl: vi.fn(
      options?.getImageDataUrl ??
        (async (imagePath: string) => `data:image/png;base64,${imagePath}`),
    ),
    applyWallpaper: vi.fn(async () => undefined),
    applyConfiguration: vi.fn(async () => undefined),
    identifyMonitors: vi.fn(async () => undefined),
    saveEditedWallpaper: vi.fn(async (monitorId: string) => `${monitorId}-edited.png`),
    log: vi.fn(async () => undefined),
  };

  return runtime;
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((nextResolve, nextReject) => {
    resolve = nextResolve;
    reject = nextReject;
  });
  return { promise, resolve, reject };
}

describe('wallpaperSession store', () => {
  it('loads monitors and profiles into a view snapshot', async () => {
    const runtime = createRuntime();
    const store = createWallpaperSessionStore({ runtime });

    await store.send({ type: 'refresh' });

    const snapshot = store.read();
    expect(snapshot.status).toBe('ready');
    expect(snapshot.monitors).toHaveLength(2);
    expect(snapshot.profiles).toEqual(['Desk']);
    expect(snapshot.dirtyCount).toBe(0);
    expect(snapshot.monitors[0]?.monitor.id).toBe('DISPLAY1');
  });

  it('applies global fit mode across the wallpaper session', async () => {
    const runtime = createRuntime();
    const store = createWallpaperSessionStore({ runtime });

    await store.send({ type: 'refresh' });
    await store.send({ type: 'set-fit-mode', fitMode: 'Span' });

    const snapshot = store.read();
    expect(snapshot.monitors.map((monitor) => monitor.draft.fitMode)).toEqual(['Span', 'Span']);
    expect(snapshot.dirtyCount).toBe(2);
  });

  it('deduplicates preview resolution requests', async () => {
    const previewRequest = deferred<string>();
    const runtime = createRuntime({
      getImageDataUrl: vi.fn(async () => previewRequest.promise),
    });
    const store = createWallpaperSessionStore({ runtime });

    const first = store.send({ type: 'resolve-preview', imagePath: 'shared.png' });
    const second = store.send({ type: 'resolve-preview', imagePath: 'shared.png' });

    previewRequest.resolve('data:image/png;base64,shared');

    await expect(first).resolves.toBe('data:image/png;base64,shared');
    await expect(second).resolves.toBe('data:image/png;base64,shared');
    expect(runtime.getImageDataUrl).toHaveBeenCalledTimes(1);
  });

  it('opens the editor from a chosen image and saves the edited wallpaper', async () => {
    const runtime = createRuntime({
      monitors: singleMonitorFixture,
      pickImagePath: 'editor-source.png',
    });
    const store = createWallpaperSessionStore({ runtime });

    await store.send({ type: 'refresh' });
    await store.send({ type: 'open-editor', monitorId: 'DISPLAY1' });

    expect(store.read().editor.open).toBe(true);
    expect(store.read().editor.sourceImagePath).toBe('editor-source.png');

    await store.send({ type: 'save-editor', dataUrl: 'data:image/png;base64,edited' });

    const snapshot = store.read();
    expect(snapshot.editor.open).toBe(false);
    expect(snapshot.monitors[0]?.draft.imagePath).toBe('DISPLAY1-edited.png');
    expect(snapshot.dirtyCount).toBe(0);
    expect(runtime.saveEditedWallpaper).toHaveBeenCalledWith(
      'DISPLAY1',
      'data:image/png;base64,edited',
    );
    expect(runtime.applyWallpaper).toHaveBeenCalledWith(
      'DISPLAY1',
      'DISPLAY1-edited.png',
      'Fill',
    );
  });

  it('loads a profile only onto active monitors', async () => {
    const runtime = createRuntime({
      monitors: singleMonitorFixture,
    });
    const store = createWallpaperSessionStore({ runtime });

    await store.send({ type: 'refresh' });
    await store.send({ type: 'load-profile', name: 'Desk' });

    const snapshot = store.read();
    expect(snapshot.monitors).toHaveLength(1);
    expect(snapshot.monitors[0]?.draft.imagePath).toBe('profile.png');
    expect(snapshot.monitors[0]?.draft.fitMode).toBe('Span');
  });
});