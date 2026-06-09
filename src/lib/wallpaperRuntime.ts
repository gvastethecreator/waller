import type {
  FitMode,
  LogLevel,
  MonitorInfo,
  Profile,
  ProfileMonitor,
} from './types';

export interface WallpaperApplyConfiguration {
  monitorId: string;
  imagePath: string;
  fitMode: FitMode;
}

export interface WallpaperSessionRuntime {
  fetchMonitors(): Promise<MonitorInfo[]>;
  listProfiles(): Promise<string[]>;
  loadProfile(name: string): Promise<Profile>;
  saveProfile(name: string, monitors: ProfileMonitor[]): Promise<void>;
  deleteProfile(name: string): Promise<void>;
  pickImagePath(): Promise<string | null>;
  getImageDataUrl(imagePath: string): Promise<string>;
  applyWallpaper(monitorId: string, imagePath: string, fitMode: FitMode): Promise<void>;
  applyConfiguration(configs: WallpaperApplyConfiguration[]): Promise<void>;
  identifyMonitors(): Promise<void>;
  saveEditedWallpaper(monitorId: string, dataUrl: string): Promise<string>;
  log?(scope: string, message: string, level?: LogLevel): Promise<void>;
}
