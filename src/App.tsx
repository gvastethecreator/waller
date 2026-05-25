import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import {
    useEffect,
    useMemo,
    useRef,
    useState,
} from 'react';
import { EditorDialog } from './components/EditorDialog';
import { MonitorCard } from './components/MonitorCard';
import { MonitorLayout } from './components/MonitorLayout';
import { useWallpaperSession } from './hooks/useWallpaperSession';
import { useI18n } from './i18n';
import { formatError } from './lib/appErrors';
import {
    clearLogs,
    confirmDialog,
    getLogs,
    initializeLogging,
    logClient,
    tauriWallpaperSessionRuntime,
} from './lib/tauri';
import type {
    FitMode,
    ToastState,
    WallpaperSourceType,
} from './lib/types';

const IDENTIFY_FALLBACK_DELAY_MS = 700;

export default function App() {
    const gridRef = useRef<HTMLDivElement>(null);
    const toastTimerRef = useRef<number | null>(null);
    const { t, locale, setLocale } = useI18n();
    const {
        snapshot,
        session,
        monitorDrafts,
        profiles,
        editor,
        previews,
    } = useWallpaperSession({
        runtime: tauriWallpaperSessionRuntime,
        identifyFallbackDelayMs: IDENTIFY_FALLBACK_DELAY_MS,
    });

    const [selectedProfileName, setSelectedProfileName] = useState('');
    const [profileNameInput, setProfileNameInput] = useState('');
    const [toast, setToast] = useState<ToastState | null>(null);
    const [saveModalOpen, setSaveModalOpen] = useState(false);
    const [logsModalOpen, setLogsModalOpen] = useState(false);
    const [logsContent, setLogsContent] = useState('No logs yet.');

    const monitorItems = snapshot.monitors;
    const layoutMonitors = useMemo(
        () => monitorItems.map((item) => item.monitor),
        [monitorItems],
    );
    const dirtyCount = snapshot.dirtyCount;
    const statusSummary = useMemo(() => {
        if (!monitorItems.length) {
            return t('status.loading');
        }
        if (dirtyCount > 0) {
            const key = dirtyCount > 1 ? 'status.pendingChanges' : 'status.pendingChange';
            return `${t('status.displays', { count: monitorItems.length })} · ${t(key, { count: dirtyCount })}`;
        }
        return `${t('status.displays', { count: monitorItems.length })} · ${t('status.allApplied')}`;
    }, [dirtyCount, monitorItems.length, t]);
    const animationKey = useMemo(
        () => `${monitorItems.map((item) => item.monitor.id).join('|')}::${dirtyCount}`,
        [dirtyCount, monitorItems],
    );

    const pushToast = (message: string, tone: ToastState['tone']) => {
        setToast({ message, tone });
        if (toastTimerRef.current !== null) {
            window.clearTimeout(toastTimerRef.current);
        }
        toastTimerRef.current = window.setTimeout(() => {
            setToast(null);
            toastTimerRef.current = null;
        }, 3000);
    };

    useEffect(() => {
        return () => {
            if (toastTimerRef.current !== null) {
                window.clearTimeout(toastTimerRef.current);
            }
        };
    }, []);

    useGSAP(
        () => {
            const media = gsap.matchMedia();
            media.add(
                {
                    reduceMotion: '(prefers-reduced-motion: reduce)',
                },
                (context) => {
                    const reduceMotion = context.conditions?.reduceMotion;
                    if (reduceMotion) {
                        gsap.set('.js-monitor-card', { autoAlpha: 1, y: 0 });
                        return;
                    }

                    gsap.fromTo(
                        '.js-monitor-card',
                        { autoAlpha: 0, y: 12 },
                        {
                            autoAlpha: 1,
                            y: 0,
                            duration: 0.28,
                            ease: 'power2.out',
                            stagger: 0.05,
                            overwrite: 'auto',
                        },
                    );
                },
            );

            return () => media.revert();
        },
        { scope: gridRef, dependencies: [animationKey], revertOnUpdate: true },
    );

    useEffect(() => {
        void initializeLogging();
        void session.refresh(false).catch((error) => {
            pushToast(t('error.detectMonitors', { error: formatError(error) }), 'error');
        });
    }, [session, t]);

    useEffect(() => {
        const handleWindowError = (event: ErrorEvent) => {
            void logClient(
                'window-error',
                `${event.message} at ${event.filename}:${event.lineno}:${event.colno}`,
                'error',
            );
        };
        const handleRejection = (event: PromiseRejectionEvent) => {
            void logClient(
                'unhandled-rejection',
                formatError(event.reason ?? 'unknown rejection'),
                'error',
            );
        };

        window.addEventListener('error', handleWindowError);
        window.addEventListener('unhandledrejection', handleRejection);
        return () => {
            window.removeEventListener('error', handleWindowError);
            window.removeEventListener('unhandledrejection', handleRejection);
        };
    }, []);

    const handleRefresh = async () => {
        try {
            await session.refresh(true);
        } catch (error) {
            pushToast(t('error.detectMonitors', { error: formatError(error) }), 'error');
        }
    };

    const handleBrowseMonitorImage = async (monitorId: string) => {
        try {
            await monitorDrafts.chooseImage(monitorId);
        } catch (error) {
            pushToast(t('error.fileDialog', { error: formatError(error) }), 'error');
        }
    };

    const handleSourceChange = async (monitorId: string, nextType: WallpaperSourceType) => {
        try {
            await monitorDrafts.setSourceType(monitorId, nextType);
        } catch (error) {
            const message = nextType === 'image'
                ? t('error.fileDialog', { error: formatError(error) })
                : formatError(error);
            pushToast(message, 'error');
        }
    };

    const handleFitChange = async (fitMode: FitMode) => {
        try {
            await monitorDrafts.setFitMode(fitMode);
        } catch (error) {
            pushToast(formatError(error), 'error');
        }
    };

    const handleSolidColorChange = (monitorId: string, color: string) => {
        void monitorDrafts.setSolidColor(monitorId, color).catch((error) => {
            pushToast(formatError(error), 'error');
        });
    };

    const handleClearMonitor = (monitorId: string) => {
        void monitorDrafts.clear(monitorId).catch((error) => {
            pushToast(formatError(error), 'error');
        });
    };

    const handleApplyMonitor = async (monitorId: string) => {
        try {
            await monitorDrafts.apply(monitorId);
            pushToast(t('monitor.applied'), 'success');
        } catch (error) {
            pushToast(t('monitor.applyFailed', { error: formatError(error) }), 'error');
        }
    };

    const handleApplyConfiguration = async () => {
        try {
            await session.applyAll();
            pushToast(t('apply.success'), 'success');
        } catch (error) {
            pushToast(t('apply.failed', { error: formatError(error) }), 'error');
        }
    };

    const handleOpenEditor = async (monitorId: string) => {
        const monitorItem = monitorItems.find((item) => item.monitor.id === monitorId);
        if (!monitorItem) {
            pushToast(t('error.monitorNotFound'), 'error');
            return;
        }
        if (!monitorItem.canEdit) {
            pushToast(t('error.editorDiagnostic'), 'error');
            return;
        }

        try {
            await editor.open(monitorId);
        } catch (error) {
            pushToast(formatError(error), 'error');
        }
    };

    const handleIdentifyMonitors = async () => {
        if (!monitorItems.length) {
            pushToast(t('layout.noMonitors'), 'error');
            return;
        }

        try {
            await session.identify();
            pushToast(t('identify.showing'), 'success');
        } catch (error) {
            pushToast(formatError(error), 'error');
        }
    };

    const handleLoadSelectedProfile = async () => {
        if (!selectedProfileName) {
            pushToast(t('profile.selectFirst'), 'error');
            return;
        }

        try {
            await profiles.load(selectedProfileName);
            pushToast(t('profile.loaded', { name: selectedProfileName }), 'success');
        } catch (error) {
            pushToast(t('profile.loadFailed', { error: formatError(error) }), 'error');
        }
    };

    const handleSaveCurrentProfile = async () => {
        const name = profileNameInput.trim();
        if (!name) {
            pushToast(t('profile.enterName'), 'error');
            return;
        }

        try {
            await profiles.save(name);
            setSaveModalOpen(false);
            setProfileNameInput('');
            setSelectedProfileName(name);
            pushToast(t('profile.saved', { name }), 'success');
        } catch (error) {
            pushToast(t('profile.saveFailed', { error: formatError(error) }), 'error');
        }
    };

    const handleDeleteSelectedProfile = async () => {
        if (!selectedProfileName) {
            pushToast(t('profile.selectToDelete'), 'error');
            return;
        }

        const confirmed = await confirmDialog(
            t('profile.deleteConfirm', { name: selectedProfileName }),
        );
        if (!confirmed) {
            return;
        }

        try {
            await profiles.delete(selectedProfileName);
            setSelectedProfileName('');
            pushToast(t('profile.deleted', { name: selectedProfileName }), 'success');
        } catch (error) {
            pushToast(t('profile.deleteFailed', { error: formatError(error) }), 'error');
        }
    };

    const refreshLogsModal = async () => {
        try {
            const content = await getLogs();
            setLogsContent(content || t('logsModal.noLogs'));
        } catch (error) {
            setLogsContent(t('logsModal.loadFailed', { error: formatError(error) }));
        }
    };

    const handleOpenLogsModal = async () => {
        setLogsModalOpen(true);
        await refreshLogsModal();
    };

    const handleClearLogs = async () => {
        try {
            await clearLogs();
            await logClient('logs', 'logs cleared by user', 'warn');
            pushToast(t('logsModal.cleared'), 'success');
            if (logsModalOpen) {
                await refreshLogsModal();
            }
        } catch (error) {
            pushToast(t('logsModal.clearFailed', { error: formatError(error) }), 'error');
        }
    };

    return (
        <div
            className="flex min-h-screen flex-col"
            style={{ background: 'var(--bg-primary)', color: 'var(--text-primary)' }}
        >
            <header className="flex items-center justify-between gap-3 px-3 py-2" style={{ background: 'var(--bg-secondary)' }}>
                <div className="flex min-w-0 items-center gap-3">
                    <div className="flex items-center gap-2">
                        <svg
                            className="text-[#b5bac4]"
                            fill="none"
                            height="24"
                            stroke="currentColor"
                            strokeWidth="2"
                            viewBox="0 0 24 24"
                            width="24"
                        >
                            <rect height="14" rx="2" width="20" x="2" y="3" />
                            <line x1="8" x2="16" y1="21" y2="21" />
                            <line x1="12" x2="12" y1="17" y2="21" />
                        </svg>
                        <h1 className="text-sm font-semibold tracking-tight">{t('app.title')}</h1>
                    </div>
                    <span className="rounded-full bg-[#1d1f24] px-3 py-1 text-[11px] text-[#d7d9de]">
                        {statusSummary}
                    </span>
                </div>
                <div className="flex items-center gap-2">
                    <select
                        className="input-select text-xs"
                        value={locale}
                        onChange={(event) => setLocale(event.target.value as 'en' | 'es')}
                    >
                        <option value="en">EN</option>
                        <option value="es">ES</option>
                    </select>
                    <button className="btn btn-ghost" type="button" onClick={() => void handleRefresh()}>
                        {t('app.refresh')}
                    </button>
                </div>
            </header>

            <section className="border-b border-white/5 bg-[#101115] px-3 py-2">
                <div className="mx-auto flex w-full max-w-350 flex-wrap items-center gap-2">
                    <select
                        className="input-select min-w-50"
                        value={selectedProfileName}
                        onChange={(event) => setSelectedProfileName(event.target.value)}
                    >
                        <option value="">{t('profile.select')}</option>
                        {snapshot.profiles.map((profile) => (
                            <option key={profile} value={profile}>
                                {profile}
                            </option>
                        ))}
                    </select>
                    <button className="btn btn-secondary" type="button" onClick={() => void handleLoadSelectedProfile()}>
                        {t('profile.load')}
                    </button>
                    <button className="btn btn-secondary" type="button" onClick={() => setSaveModalOpen(true)}>
                        {t('profile.save')}
                    </button>
                    <button className="btn btn-danger" type="button" onClick={() => void handleDeleteSelectedProfile()}>
                        {t('profile.delete')}
                    </button>
                    <button className="btn btn-secondary" type="button" onClick={() => void handleOpenLogsModal()}>
                        {t('profile.logs')}
                    </button>
                    <button className="btn btn-secondary" type="button" onClick={() => void handleClearLogs()}>
                        {t('profile.clearLogs')}
                    </button>
                </div>
            </section>

            <main className="flex-1 overflow-y-auto px-3 py-3">
                <section className="mx-auto max-w-350">
                    <div className="mb-2 flex items-center justify-between gap-3">
                        <div>
                            <h2 className="text-[13px] font-bold uppercase tracking-[0.6px]">{t('layout.title')}</h2>
                            <p className="mt-1 text-xs" style={{ color: 'var(--text-secondary)' }}>
                                {t('layout.fitGlobal')}
                            </p>
                        </div>
                        <button className="btn btn-secondary" type="button" onClick={() => void handleIdentifyMonitors()}>
                            {t('layout.identify')}
                        </button>
                    </div>

                    {snapshot.diagnosticMode ? (
                        <p className="mb-3 rounded-md border border-[#d4c6a2]/20 bg-[#1d1a13] px-3 py-2 text-xs text-[#d4c6a2]">
                            {t('layout.diagnosticMode')}
                        </p>
                    ) : null}

                    <MonitorLayout
                        highlightedMonitorId={snapshot.identifyOverlay.highlightedMonitorId}
                        monitors={layoutMonitors}
                    />

                    <div ref={gridRef} className="mt-5 grid gap-4 xl:grid-cols-3 md:grid-cols-2">
                        {(snapshot.status === 'loading' || snapshot.status === 'refreshing') && !monitorItems.length ? (
                            <div
                                className="col-span-full flex flex-col items-center gap-3 rounded-2xl border border-white/5 px-6 py-14"
                                style={{ background: 'var(--bg-card)', color: 'var(--text-secondary)' }}
                            >
                                <div className="spinner" />
                                <p>{t('layout.detecting')}</p>
                            </div>
                        ) : null}

                        {snapshot.status !== 'loading' && snapshot.status !== 'refreshing' && !monitorItems.length ? (
                            <div
                                className="col-span-full flex flex-col items-center gap-3 rounded-2xl border border-white/5 px-6 py-14"
                                style={{ background: 'var(--bg-card)', color: 'var(--text-secondary)' }}
                            >
                                <p>{t('layout.noMonitors')}</p>
                            </div>
                        ) : null}

                        {monitorItems.map((item) => {
                            const previewUrl = item.preview.kind === 'ready' ? item.preview.dataUrl : '';
                            return (
                                <MonitorCard
                                    key={item.monitor.id}
                                    dirty={item.dirty}
                                    draft={item.draft}
                                    hasPreviewError={item.preview.kind === 'error'}
                                    highlighted={snapshot.identifyOverlay.highlightedMonitorId === item.monitor.id}
                                    isPreviewLoading={item.preview.kind === 'loading'}
                                    monitor={item.monitor}
                                    previewUrl={previewUrl}
                                    onApply={(monitorId) => void handleApplyMonitor(monitorId)}
                                    onBrowse={(monitorId) => void handleBrowseMonitorImage(monitorId)}
                                    onClear={handleClearMonitor}
                                    onEdit={(monitorId) => void handleOpenEditor(monitorId)}
                                    onFitChange={(_, fitMode) => void handleFitChange(fitMode)}
                                    onSolidColorChange={handleSolidColorChange}
                                    onSourceChange={(monitorId, nextType) => void handleSourceChange(monitorId, nextType)}
                                />
                            );
                        })}
                    </div>
                </section>
            </main>

            <footer className="flex items-center justify-center px-3 py-3" style={{ background: 'var(--bg-secondary)' }}>
                <button className="btn btn-primary min-w-65 justify-center" type="button" onClick={() => void handleApplyConfiguration()}>
                    {t('apply.button')}
                </button>
            </footer>

            {saveModalOpen ? (
                <div className="fixed inset-0 z-40 flex items-center justify-center">
                    <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setSaveModalOpen(false)} />
                    <section
                        className="relative w-95 rounded-2xl border border-white/8 p-6 shadow-xl"
                        style={{ background: 'var(--bg-card)', boxShadow: 'var(--shadow-lg)' }}
                    >
                        <h2 className="mb-4 text-base font-semibold">{t('saveModal.title')}</h2>
                        <input
                            autoFocus
                            className="input-field"
                            placeholder={t('profile.namePlaceholder')}
                            type="text"
                            value={profileNameInput}
                            onChange={(event) => setProfileNameInput(event.target.value)}
                            onKeyDown={(event) => {
                                if (event.key === 'Enter') {
                                    void handleSaveCurrentProfile();
                                }
                                if (event.key === 'Escape') {
                                    setSaveModalOpen(false);
                                }
                            }}
                        />
                        <div className="mt-4 flex justify-end gap-2">
                            <button className="btn btn-secondary" type="button" onClick={() => setSaveModalOpen(false)}>
                                {t('saveModal.cancel')}
                            </button>
                            <button className="btn btn-primary" type="button" onClick={() => void handleSaveCurrentProfile()}>
                                {t('saveModal.save')}
                            </button>
                        </div>
                    </section>
                </div>
            ) : null}

            {logsModalOpen ? (
                <div className="fixed inset-0 z-40 flex items-center justify-center">
                    <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setLogsModalOpen(false)} />
                    <section
                        className="relative flex h-[min(74vh,720px)] w-[min(92vw,980px)] flex-col rounded-2xl border border-white/8 p-6 shadow-xl"
                        style={{ background: 'var(--bg-card)', boxShadow: 'var(--shadow-lg)' }}
                    >
                        <h2 className="mb-4 text-base font-semibold">{t('logsModal.title')}</h2>
                        <pre className="logs-view flex-1">{logsContent}</pre>
                        <div className="mt-4 flex justify-end gap-2">
                            <button className="btn btn-secondary" type="button" onClick={() => void refreshLogsModal()}>
                                {t('logsModal.refresh')}
                            </button>
                            <button className="btn btn-primary" type="button" onClick={() => setLogsModalOpen(false)}>
                                {t('logsModal.close')}
                            </button>
                        </div>
                    </section>
                </div>
            ) : null}

            <EditorDialog
                fitMode={snapshot.editor.fitMode}
                monitor={snapshot.editor.monitor}
                open={snapshot.editor.open}
                resolvePreviewDataUrl={previews.resolveDataUrl}
                sourceImagePath={snapshot.editor.sourceImagePath}
                onClose={() => void editor.close()}
                onPickImage={editor.pickImage}
                onSave={async ({ dataUrl }) => {
                    await editor.save(dataUrl);
                    pushToast(t('editor.saved'), 'success');
                }}
            />

            {toast ? (
                <div className={`toast toast-${toast.tone}`}>
                    {toast.message}
                </div>
            ) : null}
        </div>
    );
}
