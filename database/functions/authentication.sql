-- =============================================================================
-- LabTrack Security & Authentication Schemas
-- Database Engine: PostgreSQL 14+
-- Module: Users, Roles, Refresh Tokens, and Internal Service Verification
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: roles
-- Purpose: Defines access level permissions (e.g., Admin, Technician, Researcher)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS roles (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Initial Roles Data
INSERT INTO roles (name, description) VALUES
    ('Admin', 'Full administrative access across all laboratory services and user management'),
    ('Technician', 'Maintenance, equipment repair ticketing, and diagnostic telemetry access'),
    ('Researcher', 'Standard operational user; asset booking, safety evaluation, and AI assistance')
ON CONFLICT (name) DO NOTHING;

-- -----------------------------------------------------------------------------
-- Table: users
-- Purpose: Core platform user identity records
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_role_id ON users(role_id);

-- -----------------------------------------------------------------------------
-- Table: refresh_tokens
-- Purpose: Stores active JWT refresh tokens for secure session re-authentication
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    replaced_by_token VARCHAR(255)
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_hash ON refresh_tokens(token_hash);

-- -----------------------------------------------------------------------------
-- Table: internal_service_keys
-- Purpose: Holds HMAC secrets for internal microservice-to-microservice authentication
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS internal_service_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name VARCHAR(100) NOT NULL UNIQUE,
    secret_key_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Seed initial internal gateway identity
INSERT INTO internal_service_keys (service_name, secret_key_hash) VALUES
    ('LabTrack.CSharpGateway', '$2b$12$e868d/9JpI26O8jDk2/Y0.vA6K9b5R2G08G39G2.9zR2m4N8u2E4i')
ON CONFLICT (service_name) DO NOTHING;

COMMIT;