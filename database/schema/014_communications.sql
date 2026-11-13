-- =============================================================================
-- LabTrack Schema Initialization: 014_communications.sql (Continued)
-- Database Engine: PostgreSQL 14+
-- Module: Inter-Departmental Messaging, Ticket Thread Discussions, Broadcast Alerts,
--          & Notification Preferences
-- =============================================================================

    tc.created_at,
    tc.updated_at
FROM 
    maintenance.ticket_comments tc
JOIN 
    auth.users u ON tc.author_user_id = u.id
JOIN 
    auth.roles r ON u.role_id = r.id
ORDER BY 
    tc.created_at ASC;


-- -----------------------------------------------------------------------------
-- 5. View: audit.v_active_facility_broadcasts
-- Purpose: Aggregates active laboratory announcements and location contexts
--          for display on client portals and dashboard banners.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW audit.v_active_facility_broadcasts AS
SELECT 
    b.id AS broadcast_id,
    b.title,
    b.body,
    b.severity,
    b.facility_id,
    f.name AS facility_name,
    b.laboratory_id,
    l.room_number AS lab_room_number,
    l.name AS laboratory_name,
    u.full_name AS publisher_name,
    b.display_from,
    b.display_until,
    b.created_at
FROM 
    audit.facility_broadcast_announcements b
LEFT JOIN 
    inventory.facilities f ON b.facility_id = f.id
LEFT JOIN 
    inventory.laboratories l ON b.laboratory_id = l.id
JOIN 
    auth.users u ON b.publisher_user_id = u.id
WHERE 
    b.is_active = TRUE
    AND NOW() BETWEEN b.display_from AND b.display_until
ORDER BY 
    CASE b.severity 
        WHEN 'Emergency' THEN 1 
        WHEN 'Critical' THEN 2 
        WHEN 'Warning' THEN 3 
        WHEN 'Info' THEN 4 
    END ASC,
    b.created_at DESC;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE maintenance.ticket_comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.facility_broadcast_announcements ENABLE ROW LEVEL SECURITY;
ALTER TABLE auth.user_notification_preferences ENABLE ROW LEVEL SECURITY;

-- Comments Read Policy: Users see external comments; internal notes restricted to Admins/Technicians
DROP POLICY IF EXISTS p_ticket_comments_read ON maintenance.ticket_comments;
CREATE POLICY p_ticket_comments_read ON maintenance.ticket_comments
    FOR SELECT
    USING (
        (is_internal_only = FALSE)
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

-- Comments Insert Policy: Authenticated users can post comments```sql
-- =============================================================================
-- LabTrack Schema Initialization: 014_communications.sql
-- Database Engine: PostgreSQL 14+
-- Module: Inter-Departmental Messaging, Ticket Direct Discussions, Broadcast Alerts,
--          & Notification Preferences (Continuation)
-- =============================================================================

    tc.created_at,
    tc.updated_at
FROM 
    maintenance.ticket_comments tc
JOIN 
    auth.users u ON tc.author_user_id = u.id
JOIN 
    auth.roles r ON u.role_id = r.id
ORDER BY 
    tc.created_at ASC;


-- -----------------------------------------------------------------------------
-- 5. View: audit.v_active_facility_broadcasts
-- Purpose: Retrieves current active announcements and emergency advisories.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW audit.v_active_facility_broadcasts AS
SELECT 
    b.id AS broadcast_id,
    b.title,
    b.body,
    b.severity,
    b.facility_id,
    f.name AS facility_name,
    b.laboratory_id,
    l.room_number,
    l.name AS laboratory_name,
    u.full_name AS publisher_name,
    b.display_from,
    b.display_until,
    b.created_at
FROM 
    audit.facility_broadcast_announcements b
LEFT JOIN 
    inventory.facilities f ON b.facility_id = f.id
LEFT JOIN 
    inventory.laboratories l ON b.laboratory_id = l.id
JOIN 
    auth.users u ON b.publisher_user_id = u.id
WHERE 
    b.is_active = TRUE
    AND NOW() BETWEEN b.display_from AND b.display_until
ORDER BY 
    CASE b.severity
        WHEN 'Emergency' THEN 1
        WHEN 'Critical' THEN 2
        WHEN 'Warning' THEN 3
        WHEN 'Info' THEN 4
    END ASC,
    b.display_from DESC;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE maintenance.ticket_comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.facility_broadcast_announcements ENABLE ROW LEVEL SECURITY;
ALTER TABLE auth.user_notification_preferences ENABLE ROW LEVEL SECURITY;

-- Ticket Comments Read Policy: Hide internal notes from Researchers/Guests
DROP POLICY IF EXISTS p_ticket_comments_read ON maintenance.ticket_comments;
CREATE POLICY p_ticket_comments_read ON maintenance.ticket_comments
    FOR SELECT
    USING (
        is_internal_only = FALSE
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician', 'Auditor')
        )
    );

-- Ticket Comments Insert Policy: Authenticated users can comment on accessible tickets
DROP POLICY IF EXISTS p_ticket_comments_insert ON maintenance.ticket_comments;
CREATE POLICY p_ticket_comments_insert ON maintenance.ticket_comments
    FOR INSERT
    WITH CHECK (
        author_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

-- Facility Broadcasts Read Policy: Public read for active users
DROP POLICY IF EXISTS p_broadcasts_read ON audit.facility_broadcast_announcements;
CREATE POLICY p_broadcasts_read ON audit.facility_broadcast_announcements
    FOR SELECT
    USING (TRUE);

-- Broadcasts Manage Policy: Only Admins/Technicians can post broadcasts
DROP POLICY IF EXISTS p_broadcasts_write ON audit.facility_broadcast_announcements;
CREATE POLICY p_broadcasts_write ON audit.facility_broadcast_announcements
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

-- Notification Preferences Policy: Users manage their own preferences
DROP POLICY IF EXISTS p_notification_prefs_self ON auth.user_notification_preferences;
CREATE POLICY p_notification_prefs_self ON auth.user_notification_preferences
    FOR ALL
    USING (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

COMMIT;