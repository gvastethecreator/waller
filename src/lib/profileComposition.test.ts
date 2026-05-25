import { describe, expect, it } from 'vitest';
import type { Profile, WallpaperDraft } from './types';
import {
  composeProfileSave,
  createDraftsFromProfile,
  validateProfileMonitors,
  validateProfileName,
} from './profileComposition';

const activeMonitors = [
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
] as const;

describe('profileComposition', () => {
  it('prepares a trimmed profile save payload from wallpaper drafts', () => {
    const drafts: Record<string, WallpaperDraft> = {
      DISPLAY1: { imagePath: 'updated.png', fitMode: 'Span' },
      DISPLAY2: { imagePath: '__SOLID__:#112233', fitMode: 'Fill' },
    };

    expect(composeProfileSave('  Desk  ', drafts)).toEqual({
      name: 'Desk',
      monitors: [
        { monitorId: 'DISPLAY1', imagePath: 'updated.png', fitMode: 'Span' },
        { monitorId: 'DISPLAY2', imagePath: '__SOLID__:#112233', fitMode: 'Fill' },
      ],
    });
  });

  it('projects a profile only onto active monitors', () => {
    const profile: Profile = {
      profileName: 'Desk',
      monitors: [
        { monitorId: 'DISPLAY1', imagePath: 'profile.png', fitMode: 'Span' },
        { monitorId: 'MISSING', imagePath: 'ignored.png', fitMode: 'Fill' },
      ],
    };

    expect(createDraftsFromProfile(profile, activeMonitors)).toEqual({
      DISPLAY1: { imagePath: 'profile.png', fitMode: 'Span' },
      DISPLAY2: { imagePath: '__NONE__', fitMode: 'Fit' },
    });
  });

  it('rejects invalid names and invalid monitor payloads', () => {
    expect(() => validateProfileName('   ')).toThrow('Profile name is required');
    expect(() =>
      validateProfileMonitors([
        { monitorId: 'DISPLAY1', imagePath: 'a.png', fitMode: 'Whatever' as never },
      ]),
    ).toThrow('Unsupported fit mode in profile: Whatever');
  });
});
