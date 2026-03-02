const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './specs',
  timeout: 90000,
  fullyParallel: false,
  reporter: [['list']],
  use: {
    baseURL: process.env.API_BASE_URL || 'http://127.0.0.1:5188'
  },
  webServer: {
    command: 'dotnet run --project ../../src/API --urls http://127.0.0.1:5188',
    url: 'http://127.0.0.1:5188/swagger/index.html',
    timeout: 120000,
    reuseExistingServer: !process.env.CI
  }
});
