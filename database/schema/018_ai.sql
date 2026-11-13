-- =============================================================================
-- LabTrack Schema Initialization: 018_ai.sql
-- Database Engine: PostgreSQL 14+ (pgvector optional)
-- Module: Diagnostic Vector Embeddings, LLM Chat Sessions, & AI Assistant Logs
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Table: ai.knowledge_embeddings
-- Purpose: Vector store for semantic documentation search, manual indexes,
--          and error diagnostic lookup. Uses pgvector if available, falling 
--          back to standard text storage if unavailable.
-- -----------------------------------------------------------------------------
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
        CREATE TABLE IF NOT EXISTS ai.knowledge_embeddings (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            asset_id UUID REFERENCES inventory.assets(id) ON DELETE CASCADE,
            title VARCHAR(255) NOT NULL,
            content_chunk TEXT NOT NULL,
            embedding vector(1536), -- Standard 1536-dim vector (e.g., OpenAI text-embedding-3-small)
            source_document VARCHAR(255),
            chunk_index INT DEFAULT 0,
            metadata JSONB DEFAULT '{}'::jsonb,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
        );
        -- Create HNSW index for high-performance approximate nearest neighbor search
        EXECUTE 'CREATE INDEX IF NOT EXISTS idx_knowledge_embeddings_hnsw ON ai.knowledge_embeddings USING hnsw (embedding vector_cosine_ops);';
    ELSE
        CREATE TABLE IF NOT EXISTS ai.knowledge_embeddings (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            asset_id UUID REFERENCES inventory.assets(id) ON DELETE CASCADE,
            title VARCHAR(255) NOT NULL,
            content_chunk TEXT NOT NULL,
            embedding TEXT, -- Fallback text representation
            source_document VARCHAR(255),
            chunk_index INT DEFAULT 0,
            metadata JSONB DEFAULT '{}'::jsonb,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
        );
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_knowledge_embeddings_asset ON ai.knowledge_embeddings(asset_id);

DROP TRIGGER IF EXISTS trg_knowledge_embeddings_update_timestamp ON ai.knowledge_embeddings;
CREATE TRIGGER trg_knowledge_embeddings_update_timestamp
BEFORE UPDATE ON ai.knowledge_embeddings
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 2. Table: ai.chat_sessions
-- Purpose: Tracks interactive AI diagnostic assistant conversations, context windows,
--          and user feedback per session.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ai.chat_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    asset_id UUID REFERENCES inventory.assets(id) ON DELETE SET NULL,
    repair_ticket_id UUID REFERENCES maintenance.repair_tickets(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL DEFAULT 'New Diagnostic Chat',
    model_name VARCHAR(100) NOT NULL DEFAULT 'gpt-4o',
    system_prompt TEXT,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_chat_sessions_user ON ai.chat_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_chat_sessions_asset ON ai.chat_sessions(asset_id);
CREATE INDEX IF NOT EXISTS idx_chat_sessions_repair_ticket ON ai.chat_sessions(repair_ticket_id);

DROP TRIGGER IF EXISTS trg_chat_sessions_update_timestamp ON ai.chat_sessions;
CREATE TRIGGER trg_chat_sessions_update_timestamp
BEFORE UPDATE ON ai.chat_sessions
FOR EACH ROW
EXECUTE FUNCTION public.fn_update_timestamp();


-- -----------------------------------------------------------------------------
-- 3. Table: ai.chat_messages
-- Purpose: Individual messages exchanged within an AI assistant thread,
--          including token counts, roles, and citations.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ai.chat_messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    session_id UUID NOT NULL REFERENCES ai.chat_sessions(id) ON DELETE CASCADE,
    sender_role VARCHAR(20) NOT NULL CHECK (
        sender_role IN ('user', 'assistant', 'system', 'tool')
    ),
    message_content TEXT NOT NULL,
    prompt_tokens INT DEFAULT 0,
    completion_tokens INT DEFAULT 0,
    cited_embedding_ids UUID[] DEFAULT '{}',
    metadata JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_chat_messages_session ON ai.chat_messages(session_id);
CREATE INDEX IF NOT EXISTS idx_chat_messages_created_at ON ai.chat_messages(created_at ASC);


-- -----------------------------------------------------------------------------
-- 4. View: ai.v_active_chat_summaries
-- Purpose: Summarizes chat threads, token usage metrics, and linked asset context.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE VIEW ai.v_active_chat_summaries AS
SELECT 
    s.id AS session_id,
    s.user_id,
    u.full_name AS user_name,
    u.email AS user_email,
    s.title AS session_title,
    s.model_name,
    s.asset_id,
    a.name AS asset_name,
    s.repair_ticket_id,
    t.ticket_number,
    COUNT(m.id) AS total_messages,
    COALESCE(SUM(m.prompt_tokens + m.completion_tokens), 0) AS total_tokens_used,
    s.created_at,
    s.updated_at
FROM 
    ai.chat_sessions s
JOIN 
    auth.users u ON s.user_id = u.id
LEFT JOIN 
    inventory.assets a ON s.asset_id = a.id
LEFT JOIN 
    maintenance.repair_tickets t ON s.repair_ticket_id = t.id
LEFT JOIN 
    ai.chat_messages m ON s.id = m.session_id
WHERE 
    s.is_archived = FALSE
GROUP BY 
    s.id, s.user_id, u.full_name, u.email, s.title, s.model_name, s.asset_id, a.name, s.repair_ticket_id, t.ticket_number, s.created_at, s.updated_at;


-- -----------------------------------------------------------------------------
-- 5. Row Level Security Policies
-- -----------------------------------------------------------------------------
ALTER TABLE ai.knowledge_embeddings ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai.chat_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai.chat_messages ENABLE ROW LEVEL SECURITY;

-- Knowledge Embeddings Policy: Read accessible to all authenticated users
DROP POLICY IF EXISTS p_knowledge_embeddings_read ON ai.knowledge_embeddings;
CREATE POLICY p_knowledge_embeddings_read ON ai.knowledge_embeddings
    FOR SELECT
    USING (TRUE);

-- Chat Sessions Policy: Users access their own sessions; Admins/Technicians view all
DROP POLICY IF EXISTS p_chat_sessions_self_read ON ai.chat_sessions;
CREATE POLICY p_chat_sessions_self_read ON ai.chat_sessions
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

DROP POLICY IF EXISTS p_chat_sessions_insert ON ai.chat_sessions;
CREATE POLICY p_chat_sessions_insert ON ai.chat_sessions
    FOR INSERT
    WITH CHECK (
        user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
    );

-- Chat Messages Policy: Tied to session access
DROP POLICY IF EXISTS p_chat_messages_read ON ai.chat_messages;
CREATE POLICY p_chat_messages_read ON ai.chat_messages
    FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM ai.chat_sessions s
            WHERE s.id = session_id
              AND (
                s.user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                OR EXISTS (
                    SELECT 1 FROM auth.users u 
                    JOIN auth.roles r ON u.role_id = r.id 
                    WHERE u.id = NULLIF(current_setting('app.current_user_id', true), '')::uuid 
                      AND r.name IN ('Admin', 'Technician')
                )
              )
        )
    );

COMMIT;