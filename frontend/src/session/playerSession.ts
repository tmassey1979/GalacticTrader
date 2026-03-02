const PlayerIdStorageKey = "gt-player-id";

export function getOrCreatePlayerId(
  storage: Pick<Storage, "getItem" | "setItem"> = window.localStorage,
  createId: () => string = () => crypto.randomUUID()
): string {
  const existing = storage.getItem(PlayerIdStorageKey);
  if (existing) {
    return existing;
  }

  const next = createId();
  storage.setItem(PlayerIdStorageKey, next);
  return next;
}
