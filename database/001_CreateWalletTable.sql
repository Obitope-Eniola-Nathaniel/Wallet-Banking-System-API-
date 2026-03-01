-- Wallet Banking API - Phase 1: Wallet table
-- Run this script in your PostgreSQL database before starting the API.
-- Example: psql -U postgres -d WalletBanking -f 001_CreateWalletTable.sql

CREATE TABLE IF NOT EXISTS "Wallet" (
    "Id"          UUID PRIMARY KEY,
    "OwnerName"   VARCHAR(255) NOT NULL,
    "Balance"     NUMERIC(18, 4) NOT NULL DEFAULT 0 CHECK ("Balance" >= 0),
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "IsDeleted"   BOOLEAN NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS "IX_Wallet_IsDeleted" ON "Wallet" ("IsDeleted");
CREATE INDEX IF NOT EXISTS "IX_Wallet_CreatedAt" ON "Wallet" ("CreatedAt" DESC);

COMMENT ON TABLE "Wallet" IS 'Phase 1: Wallet CRUD. Balance in decimal for currency accuracy.';
