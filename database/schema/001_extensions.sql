-- =============================================================================
-- LabTrack Schema Initialization: 001_extensions.sql
-- Database Engine: PostgreSQL 14+
-- Module: Required Database Extensions & Core Vector/GIST Capabilities
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Essential Core Extensions
-- -----------------------------------------------------------------------------

-- Enable UUID generation functions (uuid_generate_v4()) for primary key generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable GiST indexing support for scalar types to enforce temporal reservation exclusion constraints
CREATE EXTENSION IF NOT EXISTS "btree_gist";

-- Enable cryptographic hashing functions (SHA-256, Digest) for forensic block-chaining and security ledgers
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Enable case-insensitive text data type for uniform email and code comparisons
CREATE EXTENSION IF NOT EXISTS "citext";


-- -----------------------------------------------------------------------------
-- 2. Optional AI Vector Extension
-- -----------------------------------------------------------------------------

-- Attempt loading pgvector for AI diagnostic vector embeddings and HNSW semantic similarity search
DO $$
BEGIN
    CREATE EXTENSION IF NOT EXISTS "vector";
    RAISE NOTICE 'Extension "vector" (pgvector) successfully initialized.';
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'Extension "vector" is unavailable on this PostgreSQL instance. AI knowledge embedding vector features will operate in fallback mode.';
END $$;

COMMIT;