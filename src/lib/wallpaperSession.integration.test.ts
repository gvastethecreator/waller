import { describe, expect, it, vi } from 'vitest';
import type { MonitorInfo, Profile } from './types';
import { createWallpaperSessionStore } from './wallpaperSession';
import type { WallpaperSessionRuntime } from './wallpaperRuntime';

const monitors: MonitorInfo[] = [
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

const deskProfile: Profile = {
  profileName: 'Desk',
  monitors: [
    {
      monitorId: 'DISPLAY1',
      imagePath: 'profile.png',
      fitMode: 'Span',
    },
    {
      monitorId: 'DISPLAY2',
      imagePath: '__SOLID__:#112233',
      fitMode: 'Fill',
    },
  ],
};

function createRuntime(): WallpaperSessionRuntime {
  const profiles = ['Desk'];

  return {
    fetchMonitors: vi.fn(async () => monitors),
    listProfiles: vi.fn(async () => [...profiles]),
    loadProfile: vi.fn(async () => deskProfile),
    saveProfile: vi.fn(async (name: string) => {
      if (!profiles.includes(name)) {
        profiles.push(name);
      }
    }),
    deleteProfile: vi.fn(async () => undefined),
    pickImagePath: vi.fn(async () => null),
    getImageDataUrl: vi.fn(async (imagePath: string) => `data:image/png;base64,${imagePath}`),
    applyWallpaper: vi.fn(async () => undefined),
    applyConfiguration: vi.fn(async () => undefined),
    identifyMonitors: vi.fn(async () => undefined),
    saveEditedWallpaper: vi.fn(async (monitorId: string) => `${monitorId}-edited.png`),
    log: vi.fn(async () => undefined),
  };
}

describe('wallpaperSession integration flow', () => {
  it('loads, previews, edits, applies, and saves a profile through the wallpaper session seam', async () => {
    const runtime = createRuntime();
    const store = createWallpaperSessionStore({ runtime });

    await store.send({ type: 'refresh' });
    await store.send({ type: 'load-profile', name: 'Desk' });

    expect(store.read().monitors.map((entry) => entry.draft)).toEqual([
      { imagePath: 'profile.png', fitMode: 'Span' },
      { imagePath: '__SOLID__:#112233', fitMode: 'Fill' },
    ]);

    await expect(store.send({ type: 'resolve-preview', imagePath: 'profile.png' })).resolves.toBe(
      'data:image/png;base64,profile.png',
    );
    expect(runtime.getImageDataUrl).toHaveBeenCalledWith('profile.png');

    await store.send({ type: 'open-editor', monitorId: 'DISPLAY1' });
    expect(store.read().editor.open).toBe(true);
    expect(store.read().editor.sourceImagePath).toBe('profile.png');

    await store.send({ type: 'save-editor', dataUrl: 'data:image/png;base64,edited' });
    await store.send({ type: 'apply-monitor', monitorId: 'DISPLAY2' });
    await store.send({ type: 'save-profile', name: '  Desk Edited  ' });

    expect(runtime.saveEditedWallpaper).toHaveBeenCalledWith(
      'DISPLAY1',
      'data:image/png;base64,edited',
    );
    expect(runtime.applyWallpaper).toHaveBeenCalledWith(
      'DISPLAY1',
      'DISPLAY1-edited.png',
      'Span',
    );
    expect(runtime.applyWallpaper).toHaveBeenCalledWith(
      'DISPLAY2',
      '__SOLID__:#112233',
      'Fill',
    );
    expect(runtime.saveProfile).toHaveBeenCalledWith('Desk Edited', [
      {
        monitorId: 'DISPLAY1',
        imagePath: 'DISPLAY1-edited.png',
        fitMode: 'Span',
      },
      {
        monitorId: 'DISPLAY2',
        imagePath: '__SOLID__:#112233',
        fitMode: 'Fill',
      },
    ]);
    expect(store.read().profiles).toEqual(['Desk', 'Desk Edited']);
    expect(store.read().dirtyCount).toBe(1);
    expect(store.read().monitors.map((entry) => entry.dirty)).toEqual([true, false]);
  });
});
