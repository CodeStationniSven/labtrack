-- =============================================================================
-- LabTrack System Notifications & Alert Delivery Schemas
-- Database Engine: PostgreSQL 14+
-- Module: In-App Alerts, Push Notifications, Event Dispatch, & Delivery Logs
-- =============================================================================

BEGIN;

-- Enable UUID extension if not present
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: system_notifications
-- Purpose: Stores targeted user notifications, equipment telemetry alerts,
--          maintenance reminders, and EHS safety compliance notices.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS system_notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    recipient_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    related_asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    notification_type VARCHAR(50) NOT NULL DEFAULT 'System' CHECK (
        notification_type IN ('System', 'Maintenance', 'SafetyAlert', 'Reservation', 'Telemetry')
    ),
    priority VARCHAR(30) NOT NULL DEFAULT 'Medium' CHECK (
        priority IN ('Low', 'Medium', 'High', 'Critical')
    ),
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    action_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Performance Indexing for fast user inbox lookups & unread notification filtering
CREATE INDEX IF NOT EXISTS idx_notifications_recipient_user 
    ON system_notifications(recipient_user_id, is_read, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_notifications_related_asset 
    ON system_notifications(related_asset_id);

CREATE INDEX IF NOT EXISTS idx_notifications_type 
    ON system_notifications(notification_type);


-- -----------------------------------------------------------------------------
-- Table: notification_delivery_logs
-- Purpose: Tracks outbound multi-channel dispatch attempts (Email, Webhook, Push)
--          for auditability and retry queue handling.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS notification_delivery_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    notification_id UUID NOT NULL REFERENCES system_notifications(id) ON DELETE CASCADE,
    channel VARCHAR(30) NOT NULL CHECK (
        channel IN ('InApp', 'Email', 'Webhook', 'Push')
    ),
    delivery_status VARCHAR(30) NOT NULL DEFAULT 'Pending' CHECK (
        delivery_status IN ('Pending', 'Sent', 'Failed', 'Retrying')
    ),
    retry_count INT NOT NULL DEFAULT 0,
    error_details TEXT,
    dispatched_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notification_logs_notification_id 
    ON notification_delivery_logs(notification_id);

CREATE INDEX IF NOT EXISTS idx_notification_logs_status 
    ON notification_delivery_logs(delivery_status);


-- -----------------------------------------------------------------------------
-- Function: fn_mark_notifications_as_read
-- Purpose: Bulk updates unread notifications for a specific user.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_mark_notifications_as_read(
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
        UPDATE system_notifications
        SET is_read = TRUE,
            read_at = NOW()
        WHERE recipient_user_id = p_user_id
          AND is_read = FALSE;
    ELSE
        -- Mark specific notifications
        UPDATE system_notifications
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
-- Function: fn_cleanup_expired_notifications
-- Purpose: Automated retention maintenance function to prune old read notifications.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_cleanup_expired_notifications(
    p_retention_days INT DEFAULT 90
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_count INT;
BEGIN
    DELETE FROM system_notifications
    WHERE is_read = TRUE
      AND created_at < (NOW() - (p_retention_days || ' days')::INTERVAL);

    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;
    RETURN v_deleted_count;
END;
$$;

COMMIT;