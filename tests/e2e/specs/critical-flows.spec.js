const { test, expect } = require('@playwright/test');

async function getAdminAuthHeaders(request) {
  const login = await request.post('/api/auth/login', {
    data: {
      username: 'viper',
      password: 'ViperDev123!'
    }
  });

  expect(login.status()).toBe(200);
  const loginBody = await login.json();
  expect(loginBody.accessToken).toBeTruthy();
  return {
    Authorization: `Bearer ${loginBody.accessToken}`
  };
}

test.describe('critical gameplay flows', () => {
  test('login and registration flow', async ({ request }) => {
    const timestamp = Date.now();
    const username = `pilot_${timestamp}`;
    const email = `${username}@gt.test`;

    const register = await request.post('/api/auth/register', {
      data: {
        username,
        email,
        password: 'WarpDrive123'
      }
    });
    expect(register.status()).toBe(201);

    const login = await request.post('/api/auth/login', {
      data: {
        username,
        password: 'WarpDrive123'
      }
    });
    expect(login.status()).toBe(200);

    const loginBody = await login.json();
    expect(loginBody.accessToken).toBeTruthy();

    const validate = await request.get(`/api/auth/validate?token=${encodeURIComponent(loginBody.accessToken)}`);
    expect(validate.status()).toBe(200);
  });

  test('trading workflow smoke', async ({ request }) => {
    const economyTick = await request.post('/api/economy/tick');
    expect(economyTick.status()).toBe(200);

    const hierarchy = await request.get('/api/economy/commodities/hierarchy');
    expect(hierarchy.status()).toBe(200);

    const tradeValidation = await request.post('/api/market/trade', {
      data: {
        buyerId: '00000000-0000-0000-0000-000000000000',
        sellerId: '00000000-0000-0000-0000-000000000000',
        marketListingId: '00000000-0000-0000-0000-000000000000',
        commodityId: '00000000-0000-0000-0000-000000000000',
        quantity: -1
      }
    });

    expect([400, 404]).toContain(tradeValidation.status());
  });

  test('navigation planning and execution flow', async ({ request }) => {
    const seed = Date.now().toString();
    const authHeaders = await getAdminAuthHeaders(request);

    const createA = await request.post('/api/navigation/sectors', {
      headers: authHeaders,
      data: {
        name: `A-${seed}`,
        x: 0,
        y: 0,
        z: 0
      }
    });
    expect(createA.status()).toBe(201);
    const sectorA = await createA.json();

    const createB = await request.post('/api/navigation/sectors', {
      headers: authHeaders,
      data: {
        name: `B-${seed}`,
        x: 30,
        y: -4,
        z: 18
      }
    });
    expect(createB.status()).toBe(201);
    const sectorB = await createB.json();

    const createRoute = await request.post('/api/navigation/routes', {
      headers: authHeaders,
      data: {
        fromSectorId: sectorA.id,
        toSectorId: sectorB.id,
        legalStatus: 'Legal',
        warpGateType: 'Stable'
      }
    });
    expect(createRoute.status()).toBe(201);

    const planned = await request.get(`/api/navigation/planning/${sectorA.id}/${sectorB.id}?mode=Standard&algorithm=dijkstra`);
    expect(planned.status()).toBe(200);
  });

  test('fleet management flow', async ({ request }) => {
    const templates = await request.get('/api/fleet/templates');
    expect(templates.status()).toBe(200);

    const payload = await templates.json();
    expect(Array.isArray(payload)).toBeTruthy();
    expect(payload.length).toBeGreaterThan(0);
  });

  test('reputation flow', async ({ request }) => {
    const playerId = '11111111-1111-1111-1111-111111111111';

    const standings = await request.get(`/api/reputation/factions/${playerId}`);
    expect(standings.status()).toBe(200);

    const decay = await request.post('/api/reputation/factions/decay?points=1');
    expect(decay.status()).toBe(200);
  });
});
