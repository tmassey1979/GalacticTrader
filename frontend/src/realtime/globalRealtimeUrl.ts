export function buildGlobalRealtimeUrl(baseUrl: string, playerId: string): string {
  const normalizedBase = baseUrl.endsWith("/") ? baseUrl.slice(0, -1) : baseUrl;
  const encodedPlayerId = encodeURIComponent(playerId);
  return `${normalizedBase}/api/communication/ws/global/global?playerId=${encodedPlayerId}`;
}
