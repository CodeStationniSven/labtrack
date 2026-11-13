-- =============================================================================
-- LabTrack Schema Initialization: 016_notifications.sql
-- Database Engine: PostgreSQL 14+
-- Module: Domain-Isolated Notifications, Outbound Channel Logs, & Scheduled Event Queues
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: audit.system_notifications
-- Purpose: Stores user-targeted in-app notifications, equipment alerts, reservation
--          confirmations, and security advisories within the domain schema.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.system_notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    recipient_user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    related_asset_id UUID REFERENCES inventory.assets(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    notification_type VARCHAR(50) NOT NULL DEFAULT 'System' CHECK (
        notification_type IN ('System', 'Maintenance', 'SafetyAlert', 'Reservation', 'Telemetry', 'Security')
    ),
    priority maintenance.ticket_priority NOT NULL DEFAULT 'Medium',
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    action_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for user notification feeds, unread badge counters, and asset alerts
CREATE INDEX IF NOT EXISTS idx_sys_notifications_recipient_read 
    ON audit.system_notifications(recipient_user_id, is_read, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_sys_notifications_asset 
    ON audit.system_notifications(related_asset_id);

CREATE INDEX IF NOT EXISTS idx_sys_notifications_type 
    ON audit.system_notifications(notification_type);


-- -----------------------------------------------------------------------------
-- 2. Table: audit.notification_delivery_logs
-- Purpose: Tracks outbound multi-channel dispatch attempts (Email, Webhook, Push)
--          and retry state counters for backend event consumers.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS audit.notification_delivery_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    notification_id UUID NOT NULL REFERENCES audit.system_notifications(id) ON DELETE CASCADE,
    channel audit.notification_channel NOT NULL DEFAULT 'InApp',
    delivery_status VARCHAR(30) NOT NULL DEFAULT 'Pending' CHECK (
        delivery_status IN ('Pending', 'Sent', 'Failed', 'Retrying')
    ),
    retry_count INT NOT NULL DEFAULT 0 CHECK (retry_count >= 0),
    error_details TEXT,
    dispatched_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notification_logs_notification 
    ON audit.notification_delivery_logs(notification_id);

CREATE INDEX IF NOT EXISTS idx_notification_logs_status 
    ON audit.notification_delivery_logs(delivery_status);


-- -----------------------------------------------------------------------------
-- 3. Function: audit.fn_mark_notifications_as_read
-- Purpose: Bulk updates unread notifications for a specified user session.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_mark_notifications_as_read(
    p_user_id UUID,
    p_notification_ids UUID[] DEFAULT NULL
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_updated_count INT;
BEGIN
    IF p_notification_ids IS NULL OR array_length(p_notification_ids, 1) IS NULL THEN
        -- Mark all unread notifications for the user
        UPDATE audit.system_notifications
        SET is_read = TRUE,
            read_at = NOW()
        WHERE recipient_user_id = p_user_id
          AND is_read = FALSE;
    ELSE
        -- Mark specific notifications
        UPDATE audit.system_notifications
        SET is_read = TRUE,
            read_at = NOW()
        WHERE recipient_user_id = p_user_id
          AND id = ANY(p_notification_ids)
          AND is_read = FALSE;
    END IF;

    GET DIAGNOSTICS v_updated_count = ROW_COUNT;
    RETURN v_updated_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 4. Function: audit.fn_cleanup_expired_notifications
-- Purpose: Automated data retention procedure to purge old read notifications.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION audit.fn_cleanup_expired_notifications(
    p_retention_days INT DEFAULT 90
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_count INT;
BEGIN
    DELETE FROM audit.system_notifications
    WHERE is_read = TRUE
      AND created_at < (NOW() - (p_retention_days || ' days')::INTERVAL);

    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$;


-- -----------------------------------------------------------------------------
-- 5. View: audit.v_user_unread_notifications
-- Purpose: Displays unread user notification streams with asset contexts.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW audit.v_user_unread_notifications AS
SELECT 
    n.id AS notification_id,
    n.recipient_user_id,
    n.title,
    n.message,
    n.notification_type,
    n.priority,
    n.action_url,
    n.related_asset_id,
    a.name AS asset_name,
    a.serial_number AS asset_serial_number,
    n.created_at
FROM 
    audit.system_notifications n
LEFT JOIN 
    inventory.assets a ON n.related_asset_id = a.id
WHERE 
    n.is_read = FALSE
ORDER BY 
    CASE n.priority 
        WHEN 'Critical' THEN 1 
        WHEN 'High' THEN 2 
        WHEN 'Medium' THEN 3 
        WHEN 'Low' THEN 4 
    END ASC,
    n.created_at DESC;


-- -----------------------------------------------------------------------------
-- 6. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE audit.system_notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit.notification_delivery_logs ENABLE ROW LEVEL SECURITY;

-- Notifications Read Policy: Users can only view their own notifications
DROP POLICY IF EXISTS p_system_notifications_read ON audit.system_notifications;
CREATE POLICY p_system_notifications_read ON audit.system_notifications
    FOR SELECT
    USING (
        recipient_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
        OR EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name = 'Admin'
        )
    );

-- Delivery Logs Read Policy: Admins and Technicians can inspect delivery queue logs
DROP POLICY IF EXISTS p_notification_logs_read ON audit.notification_delivery_logs;
CREATE POLICY p_notification_logs_read ON audit.notification_delivery_logs
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM auth.users u 
            JOIN auth.roles r ON u.role_id = r.id 
            WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
              AND r.name IN ('Admin', 'Technician')
        )
    );

COMMIT;