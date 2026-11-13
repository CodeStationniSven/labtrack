-- =============================================================================
-- LabTrack Schema Initialization: 004_users.sql
-- Database Engine: PostgreSQL 14+
-- Module: Domain-Isolated User Identity, Roles, Profiles, MFA, & Authentication Sessions
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: auth.roles
-- Purpose: Defines security roles, permissions, and administrative privilege levels.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.roles (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name auth.user_role NOT NULL UNIQUE,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Seed default application security roles
INSERT INTO auth.roles (name, description) VALUES
    ('Admin', 'Full administrative access across all laboratory services and user management'),
    ('Technician', 'Maintenance, equipment repair ticketing, and diagnostic telemetry access'),
    ('Researcher', 'Standard operational user; asset booking, safety evaluation, and AI assistance'),
    ('Auditor', 'Read-only compliance verification access across financial and forensic ledgers'),
    ('Guest', 'Restricted access for temporary observers and visiting researchers')
ON CONFLICT (name) DO NOTHING;


-- -----------------------------------------------------------------------------
-- 2. Table: auth.users
-- Purpose: Core identity registry holding authentication credentials, status flags,
--          and security profile parameters.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id INT NOT NULL REFERENCES auth.roles(id) ON DELETE RESTRICT,
    email citext NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for fast authentication lookup and role filtering
CREATE INDEX IF NOT EXISTS idx_users_email ON auth.users(email);
CREATE INDEX IF NOT EXISTS idx_users_role_id ON auth.users(role_id);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON auth.users(is_active);

-- Attach automatic timestamp and optimistic concurrency version triggers
DROP TRIGGER IF EXISTS trg_users_update_timestamp ON auth.users;
CREATE TRIGGER trg_users_update_timestamp
BEFORE UPDATE ON auth.users
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_users_increment_version ON auth.users;
CREATE TRIGGER trg_users_increment_version
BEFORE UPDATE ON auth.users
FOR EACH ROW
EXECUTE FUNCTION public.fn_increment_entity_version();


-- -----------------------------------------------------------------------------
-- 3. Table: auth.user_profiles
-- Purpose: Extended demographic data, laboratory affiliations, contact details,
--          and emergency contact specifications.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.user_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES auth.users(id) ON DELETE CASCADE,
    department VARCHAR(150),
    office_location VARCHAR(100),
    phone_number VARCHAR(30),
    emergency_contact_name VARCHAR(150),
    emergency_contact_phone VARCHAR(30),
    avatar_url VARCHAR(500),
    bio TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_user_profiles_user_id ON auth.user_profiles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_profiles_department ON auth.user_profiles(department);

DROP TRIGGER IF EXISTS trg_user_profiles_update_timestamp ON auth.user_profiles;
CREATE TRIGGER trg_user_profiles_update_timestamp
BEFORE UPDATE ON auth.user_profiles
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 4. Table: auth.user_groups & Memberships
-- Purpose: Teams, research units, and lab groups for resource limits and group access.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.user_groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    group_name VARCHAR(100) NOT NULL UNIQUE,
    lead_user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS auth.user_group_memberships (
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    group_id UUID NOT NULL REFERENCES auth.user_groups(id) ON DELETE CASCADE,
    joined_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    PRIMARY KEY (user_id, group_id)
);

CREATE INDEX IF NOT EXISTS idx_user_group_memberships_user ON auth.user_group_memberships(user_id);
CREATE INDEX IF NOT EXISTS idx_user_group_memberships_group ON auth.user_group_memberships(group_id);


-- -----------------------------------------------------------------------------
-- 5. Table: auth.user_mfa_settings
-- Purpose: Multi-factor authentication secrets, TOTP states, and backup recovery codes.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.user_mfa_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES auth.users(id) ON DELETE CASCADE,
    is_mfa_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    mfa_type auth.mfa_type NOT NULL DEFAULT 'TOTP',
    totp_secret_encrypted VARCHAR(512),
    backup_codes_hashed TEXT[] DEFAULT '{}',
    enrolled_at TIMESTAMP WITH TIME ZONE,
    last_verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_mfa_settings_user_id ON auth.user_mfa_settings(user_id);

DROP TRIGGER IF EXISTS trg_user_mfa_settings_update_timestamp ON auth.user_mfa_settings;
CREATE TRIGGER trg_user_mfa_settings_update_timestamp
BEFORE UPDATE ON auth.user_mfa_settings
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 6. Table: auth.refresh_tokens & Session Security
-- Purpose: JWT refresh tokens, active client browser sessions, and reset tokens.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS auth.refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    replaced_by_token VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON auth.refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_hash ON auth.refresh_tokens(token_hash);

CREATE TABLE IF NOT EXISTS auth.active_user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    session_token_hash VARCHAR(255) NOT NULL UNIQUE,
    ip_address VARCHAR(45) NOT NULL,
    user_agent TEXT,
    device_fingerprint VARCHAR(255),
    is_current_session BOOLEAN NOT NULL DEFAULT TRUE,
    last_activity_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_active_sessions_user_id ON auth.active_user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_active_sessions_token_hash ON auth.active_user_sessions(session_token_hash);
CREATE INDEX IF NOT EXISTS idx_active_sessions_expires_at ON auth.active_user_sessions(expires_at DESC);

CREATE TABLE IF NOT EXISTS auth.user_password_resets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    reset_token_hash VARCHAR(255) NOT NULL UNIQUE,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_password_resets_user_id ON auth.user_password_resets(user_id);
CREATE INDEX IF NOT EXISTS idx_password_resets_token_hash ON auth.user_password_resets(reset_token_hash);


-- -----------------------------------------------------------------------------
-- 7. View: auth.v_active_user_summaries
-- Purpose: Unified view consolidating active users, roles, and profiles.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW auth.v_active_user_summaries AS
SELECT 
    u.id AS user_id,
    u.email,
    u.full_name,
    r.name AS role_name,
    up.department,
    up.office_location,
    u.is_active,
    u.last_login_at,
    u.created_at
FROM 
    auth.users u
JOIN 
    auth.roles r ON u.role_id = r.id
LEFT JOIN 
    auth.user_profiles up ON u.id = up.user_id
WHERE 
    u.is_active = TRUE;


-- -----------------------------------------------------------------------------
-- 8. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE auth.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE auth.user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE auth.user_mfa_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE auth.active_user_sessions ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS p_users_self_or_admin_read ON auth.users;
CREATE POLICY p_users_self_or_admin_read ON auth.users
    FOR SELECT
    USING (
        id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_user_profiles_read ON auth.user_profiles;
CREATE POLICY p_user_profiles_read ON auth.user_profiles
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

DROP POLICY IF EXISTS p_user_mfa_self_read ON auth.user_mfa_settings;
CREATE POLICY p_user_mfa_self_read ON auth.user_mfa_settings
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

DROP POLICY IF EXISTS p_active_sessions_self_read ON auth.active_user_sessions;
CREATE POLICY p_active_sessions_self_read ON auth.active_user_sessions
    FOR SELECT
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

COMMIT;