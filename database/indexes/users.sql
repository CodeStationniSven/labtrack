-- =============================================================================
-- LabTrack User Management & Identity Access Control Schemas
-- Database Engine: PostgreSQL 14+
-- Module: User Profiles, User Groups, Account Security & Activity Audit
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: user_profiles
-- Purpose: Holds extended user demographics, laboratory affiliations, 
--          contact preferences, and emergency contact details.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
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

CREATE INDEX IF NOT EXISTS idx_user_profiles_user_id ON user_profiles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_profiles_department ON user_profiles(department);

-- -----------------------------------------------------------------------------
-- Table: user_groups
-- Purpose: Defines project teams, research groups, or lab units for multi-tenant
--          or group-level equipment reservation limits.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    group_name VARCHAR(100) NOT NULL UNIQUE,
    lead_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- Table: user_group_memberships
-- Purpose: Mapping table associating users with research or administrative groups.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_group_memberships (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    group_id UUID NOT NULL REFERENCES user_groups(id) ON DELETE CASCADE,
    joined_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    PRIMARY KEY (user_id, group_id)
);

CREATE INDEX IF NOT EXISTS idx_user_group_memberships_user ON user_group_memberships(user_id);
CREATE INDEX IF NOT EXISTS idx_user_group_memberships_group ON user_group_memberships(group_id);

-- -----------------------------------------------------------------------------
-- View: v_active_user_summaries
-- Purpose: Consolidates identity, role, department, and active session details
--          for identity management dashboards.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW v_active_user_summaries AS
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
    users u
JOIN 
    roles r ON u.role_id = r.id
LEFT JOIN 
    user_profiles up ON u.id = up.user_id
WHERE 
    u.is_active = TRUE;

COMMIT;