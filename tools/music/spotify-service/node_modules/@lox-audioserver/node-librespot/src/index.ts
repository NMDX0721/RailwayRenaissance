import fs from 'node:fs';
import path from 'node:path';

import type {
  ConnectHandle,
  ConnectEvent,
  CreateSessionOpts,
  CredentialsResult,
  LibrespotSession,
  LogEvent,
  StreamHandle,
  StreamTrackOpts,
  DownloadTrackOpts,
  DownloadHandle,
  ResolveAudioFileResult,
  SessionTokens,
} from './types';

function detectLibc(): 'gnu' | 'musl' {
  const glibcVersionRuntime =
    // @ts-expect-error
    process.report?.getReport?.()?.header?.glibcVersionRuntime;
  return glibcVersionRuntime ? 'gnu' : 'musl';
}

function platformArchABI(): string {
  const { platform, arch } = process;
  if (platform === 'linux') {
    return `linux-${arch}-${detectLibc()}`;
  }
  if (platform === 'darwin') {
    return `darwin-${arch}`;
  }
  if (platform === 'win32') {
    return `win32-${arch}-msvc`;
  }
  throw new Error(`Unsupported platform ${platform}-${arch}`);
}

function resolveNativeBinding() {
  const override = process.env.LOX_LIBRESPOT_ADDON_PATH;
  if (override && fs.existsSync(override)) {
    return override;
  }

  const prebuiltPath = path.join(
    __dirname,
    '..',
    'prebuilds',
    platformArchABI(),
    'librespot_addon.node',
  );
  if (fs.existsSync(prebuiltPath)) {
    return prebuiltPath;
  }

  const localBuildPath = path.join(__dirname, 'librespot_addon.node');
  if (fs.existsSync(localBuildPath)) {
    return localBuildPath;
  }

  throw new Error(
    `librespot_addon.node not found for ${platformArchABI()}. ` +
    'Install a prebuilt binary, build locally with "npm run build", ' +
    'or point LOX_LIBRESPOT_ADDON_PATH to the compiled addon.',
  );
}

// eslint-disable-next-line @typescript-eslint/no-var-requires
const native = require(resolveNativeBinding()) as {
  createSession(opts: CreateSessionOpts): Promise<LibrespotSession>;
  createSessionWithCredentials(
    credentialsPath: string,
    deviceName?: string | null,
    cacheDir?: string | null,
    cacheSizeLimitMb?: number | null,
  ): Promise<LibrespotSession>;
  setLogLevel(level: string): void;
  loginWithAccessToken(
    accessToken: string,
    deviceName?: string,
  ): Promise<CredentialsResult>;
  downloadTrack(
    opts: DownloadTrackOpts,
    onChunk: (chunk: Buffer) => void,
    onLog?: (event: LogEvent) => void,
  ): Promise<DownloadHandle>;
  startZeroconfLogin(
    deviceId: string,
    name?: string | null,
    timeoutMs?: number | null,
  ): Promise<CredentialsResult>;
  startConnectDevice(
    credentialsPath: string,
    name: string,
    deviceId: string,
    onChunk: (chunk: Buffer) => void,
    onEvent?: (event: ConnectEvent) => void,
    onLog?: (event: LogEvent) => void,
  ): Promise<ConnectHandle>;
  startConnectDeviceWithCredentials(
    credentialsPath: string,
    name: string,
    deviceId: string,
    onChunk: (chunk: Buffer) => void,
    onEvent?: (event: ConnectEvent) => void,
    onLog?: (event: LogEvent) => void,
  ): Promise<ConnectHandle>;
  startConnectDeviceWithToken(
    accessToken: string,
    clientId: string | undefined,
    name: string,
    deviceId: string,
    onChunk: (chunk: Buffer) => void,
    onEvent?: (event: ConnectEvent) => void,
    onLog?: (event: LogEvent) => void,
  ): Promise<ConnectHandle>;
};

function wrapStreamHandle(handle: StreamHandle) {
  return {
    stop: () => handle.stop(),
    get sampleRate() {
      return (handle as any).sampleRate ?? (handle as any).sample_rate ?? handle.sampleRate;
    },
    get channels() {
      return (handle as any).channels ?? handle.channels;
    },
  };
}

function wrapSession(session: LibrespotSession) {
  return {
    streamTrack: (
      opts: StreamTrackOpts,
      onChunk: (chunk: Buffer) => void,
      onEvent?: (event: ConnectEvent) => void,
      onLog?: (event: LogEvent) => void,
    ) => {
      const nativeOpts = {
        uri: opts.uri,
        startPositionMs: (opts as any).startPositionMs ?? (opts as any).start_position_ms,
        bitrate: opts.bitrate,
        output: (opts as any).output,
        emitEvents: (opts as any).emitEvents ?? (opts as any).emit_events,
      };
      const handle = (session as any).streamTrack(nativeOpts, onChunk, onEvent, onLog);
      return wrapStreamHandle(handle);
    },
    resolveAudioFile: (opts: DownloadTrackOpts): ResolveAudioFileResult => {
      return (session as any).resolveAudioFile({ uri: opts.uri, bitrate: opts.bitrate });
    },
    resolveAudioFileAsync: (opts: DownloadTrackOpts): Promise<ResolveAudioFileResult> => {
      return (session as any).resolveAudioFileAsync({ uri: opts.uri, bitrate: opts.bitrate });
    },
    getTokens: (): Promise<SessionTokens> => {
      return (session as any).getTokens();
    },
    close: () => session.close(),
  };
}

export function createSession(opts: CreateSessionOpts): Promise<LibrespotSession> {
  const nativeOpts: CreateSessionOpts = {
    accessToken: (opts as any).accessToken ?? (opts as any).access_token,
    clientId: (opts as any).clientId ?? (opts as any).client_id,
    deviceName: (opts as any).deviceName ?? (opts as any).device_name,
    cacheDir: opts.cacheDir,
    cacheSizeLimitMb: opts.cacheSizeLimitMb,
  };
  return native.createSession(nativeOpts).then((sess) => wrapSession(sess) as any);
}

export function createSessionWithCredentials(
  credentialsPathOrJson: string,
  deviceName?: string | null,
  cacheDir?: string | null,
  cacheSizeLimitMb?: number | null,
): Promise<LibrespotSession> {
  return native
    .createSessionWithCredentials(credentialsPathOrJson, deviceName ?? null, cacheDir ?? null, cacheSizeLimitMb ?? null)
    .then((sess) => wrapSession(sess) as any);
}

export function loginWithAccessToken(
  accessToken: string,
  deviceName?: string,
): Promise<CredentialsResult> {
  return native.loginWithAccessToken(accessToken, deviceName).then((res: any) => {
    const credentialsJson = res.credentialsJson ?? res.credentials_json;
    return {
      ...res,
      credentialsJson,
      credentials_json: credentialsJson ?? res.credentials_json,
    };
  });
}

export function startZeroconfLogin(
  deviceId: string,
  name?: string,
  timeoutMs?: number,
): Promise<CredentialsResult> {
  return native.startZeroconfLogin(deviceId, name, timeoutMs).then((res: any) => {
    const credentialsJson = res.credentialsJson ?? res.credentials_json;
    return {
      ...res,
      credentialsJson,
      credentials_json: credentialsJson ?? res.credentials_json,
    };
  });
}

export function startConnectDevice(
  credentialsPath: string,
  name: string,
  deviceId: string,
  onChunk: (chunk: Buffer) => void,
  onEvent?: (event: ConnectEvent) => void,
  onLog?: (event: LogEvent) => void,
): Promise<ConnectHandle> {
  // Legacy entrypoint kept for API compatibility; immediately fails.
  return Promise.reject(
    new Error('startConnectDevice is deprecated; use startConnectDeviceWithToken(accessToken, clientId, ...)'),
  );
}

export function startConnectDeviceWithCredentials(
  credentialsPathOrJson: string,
  name: string,
  deviceId: string,
  onChunk: (chunk: Buffer) => void,
  onEvent?: (event: ConnectEvent) => void,
  onLog?: (event: LogEvent) => void,
): Promise<ConnectHandle> {
  return Promise.resolve(
    native.startConnectDeviceWithCredentials(credentialsPathOrJson, name, deviceId, onChunk, onEvent, onLog),
  ).then((handle: ConnectHandle & { sample_rate?: number }) => ({
    stop: () => handle.stop(),
    shutdown: () => handle.shutdown(),
    close: () => handle.close(),
    play: () => handle.play(),
    pause: () => handle.pause(),
    next: () => handle.next(),
    prev: () => handle.prev(),
    sampleRate: (handle as any).sampleRate ?? (handle as any).sample_rate ?? (handle as any).sampleRate,
    channels: (handle as any).channels,
  }));
}

export function startConnectDeviceWithToken(
  accessToken: string,
  clientId: string | undefined,
  name: string,
  deviceId: string,
  onChunk: (chunk: Buffer) => void,
  onEvent?: (event: ConnectEvent) => void,
  onLog?: (event: LogEvent) => void,
): Promise<ConnectHandle> {
  return Promise.resolve(
    native.startConnectDeviceWithToken(accessToken, clientId, name, deviceId, onChunk, onEvent, onLog),
  ).then((handle: ConnectHandle & { sample_rate?: number }) => ({
    stop: () => handle.stop(),
    shutdown: () => handle.shutdown(),
    close: () => handle.close(),
    play: () => handle.play(),
    pause: () => handle.pause(),
    next: () => handle.next(),
    prev: () => handle.prev(),
    sampleRate: (handle as any).sampleRate ?? (handle as any).sample_rate ?? (handle as any).sampleRate,
    channels: (handle as any).channels,
  }));
}

export function setLogLevel(level: string): void {
  native.setLogLevel(level);
}

export function downloadTrack(
  opts: DownloadTrackOpts,
  onChunk: (chunk: Buffer) => void,
  onLog?: (event: LogEvent) => void,
): Promise<DownloadHandle> {
  const nativeOpts = {
    uri: opts.uri,
    bitrate: opts.bitrate,
  };
  return native.downloadTrack(nativeOpts, onChunk, onLog);
}

// Export raw native binding for advanced use/debugging.
export { native };
export * from './types';
