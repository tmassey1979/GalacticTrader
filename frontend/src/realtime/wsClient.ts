import type { RealtimeEvent } from "../types";

type BatchHandler = (events: RealtimeEvent[]) => void;
type StatusHandler = (online: boolean) => void;

export class RealtimeSocketClient {
  private socket: WebSocket | null = null;
  private reconnectAttempts = 0;
  private readonly outboundQueue: string[] = [];
  private readonly inboundBuffer: RealtimeEvent[] = [];
  private flushTimer: number | null = null;
  private statusHandler: StatusHandler | null = null;

  constructor(
    private readonly url: string,
    private readonly onBatch: BatchHandler
  ) {}

  start(): void {
    window.addEventListener("online", this.handleBrowserOnline);
    window.addEventListener("offline", this.handleBrowserOffline);
    this.connect();
    this.flushTimer = window.setInterval(() => this.flushInbound(), 350);
  }

  stop(): void {
    window.removeEventListener("online", this.handleBrowserOnline);
    window.removeEventListener("offline", this.handleBrowserOffline);
    if (this.flushTimer !== null) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
    this.socket?.close();
    this.socket = null;
  }

  onStatus(handler: StatusHandler): void {
    this.statusHandler = handler;
  }

  send(payload: unknown): void {
    const encoded = JSON.stringify(payload);
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.socket.send(encoded);
      return;
    }

    this.outboundQueue.push(encoded);
  }

  private connect(): void {
    if (!navigator.onLine) {
      this.statusHandler?.(false);
      return;
    }

    this.socket = new WebSocket(this.url);

    this.socket.addEventListener("open", () => {
      this.reconnectAttempts = 0;
      this.statusHandler?.(true);
      this.flushOutbound();
    });

    this.socket.addEventListener("message", (event) => {
      try {
        const parsed = JSON.parse(event.data as string) as RealtimeEvent | RealtimeEvent[];
        if (Array.isArray(parsed)) {
          this.inboundBuffer.push(...parsed);
        } else {
          this.inboundBuffer.push(parsed);
        }
      } catch {
        // Ignore malformed payloads from transitional backend states.
      }
    });

    this.socket.addEventListener("close", () => {
      this.statusHandler?.(false);
      this.scheduleReconnect();
    });

    this.socket.addEventListener("error", () => {
      this.statusHandler?.(false);
      this.socket?.close();
    });
  }

  private flushOutbound(): void {
    if (this.socket?.readyState !== WebSocket.OPEN) {
      return;
    }

    while (this.outboundQueue.length > 0) {
      const next = this.outboundQueue.shift();
      if (!next) {
        continue;
      }
      this.socket.send(next);
    }
  }

  private flushInbound(): void {
    if (this.inboundBuffer.length === 0) {
      return;
    }

    const batch = this.inboundBuffer.splice(0, this.inboundBuffer.length);
    this.onBatch(batch);
  }

  private scheduleReconnect(): void {
    const backoffMs = Math.min(8_000, 500 * Math.pow(2, this.reconnectAttempts));
    this.reconnectAttempts += 1;
    window.setTimeout(() => this.connect(), backoffMs);
  }

  private handleBrowserOnline = (): void => {
    this.statusHandler?.(true);
    this.connect();
  };

  private handleBrowserOffline = (): void => {
    this.statusHandler?.(false);
    this.socket?.close();
  };
}
