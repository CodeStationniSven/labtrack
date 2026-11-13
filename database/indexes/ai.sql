-- =============================================================================
-- LabTrack AI & Diagnostic Chat Vector/Relational Indexing Schemas
-- Database Engine: PostgreSQL 14+ (with optional pgvector support)
-- Module: High-Performance Retrieval for AI Sessions, Embeddings, & Diagnostics
-- =============================================================================

BEGIN;

-- Enable UUID and optional pgvector extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- Table: ai_chat_sessions
-- Purpose: Persists LLM diagnostic conversations, context links, and token metrics.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ai_chat_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    context_asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    session_title VARCHAR(200) NOT NULL DEFAULT 'Diagnostic Assistant Chat',
    total_tokens_used INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for AI chat sessions
CREATE INDEX IF NOT EXISTS idx_ai_chat_sessions_user_id 
    ON ai_chat_sessions(user_id);

CREATE INDEX IF NOT EXISTS idx_ai_chat_sessions_asset_id 
    ON ai_chat_sessions(context_asset_id);

CREATE INDEX IF NOT EXISTS idx_ai_chat_sessions_created_at 
    ON ai_chat_sessions(created_at DESC);


-- -----------------------------------------------------------------------------
-- Table: ai_chat_messages
-- Purpose: Stores individual prompt-response exchanges within a chat session.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ai_chat_messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    session_id UUID NOT NULL REFERENCES ai_chat_sessions(id) ON DELETE CASCADE,
    sender_role VARCHAR(20) NOT NULL CHECK (sender_role IN ('user', 'assistant', 'system')),
    content TEXT NOT NULL,
    tokens_consumed INT DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexing for high-speed chronologically ordered message history retrieval
CREATE INDEX IF NOT EXISTS idx_ai_chat_messages_session_time 
    ON ai_chat_messages(session_id, created_at ASC);


-- -----------------------------------------------------------------------------
-- Table: ai_diagnostics_evaluations
-- Purpose: Logs AI telemetry and error log diagnostic analysis results.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ai_diagnostics_evaluations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    severity VARCHAR(30) NOT NULL CHECK (severity IN ('Low', 'Medium', 'High', 'Critical')),
    confidence_score NUMERIC(4, 3) NOT NULL DEFAULT 0.000,
    summary TEXT NOT NULL,
    recommended_actions JSONB NOT NULL DEFAULT '[]'::jsonb,
    error_logs_analyzed TEXT[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- B-Tree and GIN indexes for fast diagnostics queries
CREATE INDEX IF NOT EXISTS idx_ai_diagnostics_asset_id 
    ON ai_diagnostics_evaluations(asset_id);

CREATE INDEX IF NOT EXISTS idx_ai_diagnostics_severity 
    ON ai_diagnostics_evaluations(severity);

CREATE INDEX IF NOT EXISTS idx_ai_diagnostics_created_at 
    ON ai_diagnostics_evaluations(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_ai_diagnostics_recommended_actions 
    ON ai_diagnostics_evaluations USING gin(recommended_actions);


-- -----------------------------------------------------------------------------
-- Optional pgvector Extension & Vector Similarity Indexes
-- -----------------------------------------------------------------------------
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
        -- Create knowledge embeddings table if pgvector is available
        EXECUTE '
            CREATE TABLE IF NOT EXISTS ai_knowledge_embeddings (
                id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
                content_chunk TEXT NOT NULL,
                embedding vector(1536),
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            );
        ';

        -- Create HNSW or IVFFlat vector index for fast semantic similarity search
        EXECUTE '
            CREATE INDEX IF NOT EXISTS idx_ai_embeddings_vector 
            ON ai_knowledge_embeddings 
            USING hnsw (embedding vector_cosine_ops);
        ';
    END IF;
END $$;

COMMIT;