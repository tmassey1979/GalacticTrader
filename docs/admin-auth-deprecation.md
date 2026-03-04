# Legacy Admin Key Deprecation Plan

This document tracks removal of deprecated `X-Admin-Key` authorization for admin balance endpoints.

## Scope

- Affected endpoints: `/api/admin/balance/*`
- Preferred auth path: bearer token with `admin` role.
- Deprecated path: `X-Admin-Key` header.

## Timeline

1. 2026-03-04: Phase 1 (current)
- Legacy key auth defaults:
  - `true` only in `Development` when unset.
  - `false` in all non-development environments when unset.
- Legacy key usage is instrumented via Prometheus metric:
  - `admin_legacy_key_auth_attempts_total{result=success|invalid|disabled}`

2. 2026-04-15: Phase 2
- Staging must run with `Admin__AllowLegacyKeyAuth=false`.
- Any automation still using `X-Admin-Key` must be migrated to bearer `admin` tokens.

3. 2026-06-01: Phase 3
- Production policy target: `Admin__AllowLegacyKeyAuth=false` everywhere.
- Legacy key path remains available only behind explicit temporary override.

4. 2026-09-01: Phase 4 (removal milestone)
- Remove `X-Admin-Key` authorization code path.
- Remove `Admin__AllowLegacyKeyAuth` and `Admin__Key` runtime options.

## Migration Guidance

- Update scripts/jobs to acquire bearer token and call admin endpoints with:
  - `Authorization: Bearer <token>`
- Validate migration in staging before production cutover.
- Monitor `admin_legacy_key_auth_attempts_total` until `success` remains zero across the deprecation window.
