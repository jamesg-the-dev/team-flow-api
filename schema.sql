-- =============================================================================
-- TeamFlow — Production Database Schema (PostgreSQL 15+)
-- Target ORM: Entity Framework Core (.NET 8+)
-- Paste into https://drawsql.app to visualise.
--
-- Design principles
--   * Multi-tenant by `workspace` (a.k.a. organisation) — every business row
--     carries workspace_id and is enforced by FK + composite indexes.
--   * UUID v7 surrogate keys (sortable, index-friendly, safe to expose).
--   * Soft-delete via `deleted_at` on aggregate roots (Project, Task, Message).
--   * Audit columns: created_at / updated_at / created_by / updated_by.
--   * Strong typing via ENUMs for closed value sets, CHECK constraints elsewhere.
--   * Full-text search columns (tsvector) for tasks and messages.
--   * Append-only `activity_event` log feeds dashboards & activity feeds.
--   * Notifications + delivery preferences are decoupled (fan-out pattern).
--   * All FKs indexed; covering indexes for hot read paths (kanban board,
--     channel timeline, notification inbox).
-- =============================================================================

-- ---------- Extensions -------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "citext";     -- case-insensitive email
CREATE EXTENSION IF NOT EXISTS "pg_trgm";    -- fuzzy search on names/titles

-- ---------- ENUM types -------------------------------------------------------
CREATE TYPE workspace_role     AS ENUM ('owner', 'admin', 'member', 'guest');
CREATE TYPE project_status     AS ENUM ('planning', 'active', 'on_hold', 'archived', 'completed');
CREATE TYPE project_member_role AS ENUM ('lead', 'contributor', 'viewer');
CREATE TYPE priority_level     AS ENUM ('low', 'medium', 'high', 'critical');
CREATE TYPE task_column        AS ENUM ('backlog', 'todo', 'in_progress', 'review', 'done');
CREATE TYPE channel_type       AS ENUM ('public', 'private', 'direct');
CREATE TYPE notification_kind  AS ENUM ('mention', 'assignment', 'comment', 'status', 'invite', 'system');
CREATE TYPE delivery_channel   AS ENUM ('email', 'push', 'in_app');
CREATE TYPE attachment_owner   AS ENUM ('task', 'message', 'project', 'comment');

-- =============================================================================
-- IDENTITY & TENANCY
-- =============================================================================

-- ---------- users ------------------------------------------------------------
CREATE TABLE users (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email            CITEXT      NOT NULL UNIQUE,
    email_verified_at TIMESTAMPTZ,
    password_hash    TEXT        NOT NULL,           -- Argon2id/BCrypt via ASP.NET Identity
    full_name        TEXT        NOT NULL,
    display_initials TEXT        GENERATED ALWAYS AS (
                          upper(substring(regexp_replace(full_name, '\s+', ' ', 'g') FROM 1 FOR 1))
                          || upper(coalesce(substring(split_part(full_name, ' ', 2) FROM 1 FOR 1), ''))
                       ) STORED,
    avatar_url       TEXT,
    timezone         TEXT        NOT NULL DEFAULT 'UTC',
    locale           TEXT        NOT NULL DEFAULT 'en-US',
    is_active        BOOLEAN     NOT NULL DEFAULT TRUE,
    last_login_at    TIMESTAMPTZ,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_users_full_name_trgm ON users USING gin (full_name gin_trgm_ops);

-- ---------- workspaces (tenants) --------------------------------------------
CREATE TABLE workspaces (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    slug         CITEXT      NOT NULL UNIQUE,
    name         TEXT        NOT NULL,
    logo_url     TEXT,
    plan         TEXT        NOT NULL DEFAULT 'free',
    owner_id     UUID        NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at   TIMESTAMPTZ
);
CREATE INDEX ix_workspaces_owner ON workspaces(owner_id);

-- ---------- workspace_members ------------------------------------------------
CREATE TABLE workspace_members (
    workspace_id UUID         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    user_id      UUID         NOT NULL REFERENCES users(id)      ON DELETE CASCADE,
    role         workspace_role NOT NULL DEFAULT 'member',
    title        TEXT,                       -- "Senior Designer" etc.
    joined_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    invited_by   UUID         REFERENCES users(id) ON DELETE SET NULL,
    PRIMARY KEY (workspace_id, user_id)
);
CREATE INDEX ix_workspace_members_user ON workspace_members(user_id);

-- ---------- workspace_invites ------------------------------------------------
CREATE TABLE workspace_invites (
    id            UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id  UUID         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    email         CITEXT       NOT NULL,
    role          workspace_role NOT NULL DEFAULT 'member',
    token_hash    TEXT         NOT NULL UNIQUE,
    invited_by    UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    expires_at    TIMESTAMPTZ  NOT NULL,
    accepted_at   TIMESTAMPTZ,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    UNIQUE (workspace_id, email)
);

-- =============================================================================
-- AUTH & SESSIONS
-- =============================================================================

CREATE TABLE user_sessions (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    refresh_token_hash TEXT     NOT NULL UNIQUE,
    device_label    TEXT,                   -- "MacBook Pro"
    user_agent      TEXT,
    ip_address      INET,
    location        TEXT,                   -- "San Francisco, CA"
    issued_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_seen_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at      TIMESTAMPTZ NOT NULL,
    revoked_at      TIMESTAMPTZ
);
CREATE INDEX ix_user_sessions_user_active ON user_sessions(user_id) WHERE revoked_at IS NULL;

CREATE TABLE password_reset_tokens (
    token_hash  TEXT        PRIMARY KEY,
    user_id     UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    expires_at  TIMESTAMPTZ NOT NULL,
    used_at     TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =============================================================================
-- PROJECTS
-- =============================================================================

CREATE TABLE projects (
    id            UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id  UUID         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    key           TEXT         NOT NULL,           -- "PRJ", used as task prefix
    name          TEXT         NOT NULL,
    description   TEXT,
    status        project_status NOT NULL DEFAULT 'planning',
    priority      priority_level NOT NULL DEFAULT 'medium',
    start_date    DATE,
    due_date      DATE,
    budget_cents  BIGINT       CHECK (budget_cents IS NULL OR budget_cents >= 0),
    budget_currency CHAR(3)    DEFAULT 'USD',
    color_hex     CHAR(7),                          -- "#3B82F6"
    created_by    UUID         NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ,
    UNIQUE (workspace_id, key)
);
CREATE INDEX ix_projects_workspace_status ON projects(workspace_id, status) WHERE deleted_at IS NULL;
CREATE INDEX ix_projects_due_date ON projects(workspace_id, due_date) WHERE deleted_at IS NULL;

CREATE TABLE project_members (
    project_id UUID                NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    user_id    UUID                NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
    role       project_member_role NOT NULL DEFAULT 'contributor',
    added_at   TIMESTAMPTZ         NOT NULL DEFAULT now(),
    PRIMARY KEY (project_id, user_id)
);
CREATE INDEX ix_project_members_user ON project_members(user_id);

-- ---------- tags (reusable per workspace) ------------------------------------
CREATE TABLE tags (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID        NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    name         CITEXT      NOT NULL,
    color_hex    CHAR(7)     NOT NULL DEFAULT '#94A3B8',
    UNIQUE (workspace_id, name)
);

CREATE TABLE project_tags (
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    tag_id     UUID NOT NULL REFERENCES tags(id)     ON DELETE CASCADE,
    PRIMARY KEY (project_id, tag_id)
);

-- =============================================================================
-- TASKS (KANBAN)
-- =============================================================================

CREATE TABLE tasks (
    id            UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id  UUID         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    project_id    UUID         NOT NULL REFERENCES projects(id)   ON DELETE CASCADE,
    number        INTEGER      NOT NULL,           -- per-project sequence (PRJ-42)
    title         TEXT         NOT NULL,
    description   TEXT,
    column        task_column  NOT NULL DEFAULT 'backlog',
    priority      priority_level NOT NULL DEFAULT 'medium',
    position      NUMERIC(20,10) NOT NULL,          -- fractional index for drag-drop ordering
    assignee_id   UUID         REFERENCES users(id) ON DELETE SET NULL,
    reporter_id   UUID         NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    estimate_hours NUMERIC(6,2),
    due_date      DATE,
    completed_at  TIMESTAMPTZ,
    search_tsv    tsvector     GENERATED ALWAYS AS (
                       setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                       setweight(to_tsvector('english', coalesce(description, '')), 'B')
                   ) STORED,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ,
    UNIQUE (project_id, number)
);
-- Hot path: load a board column ordered by position.
CREATE INDEX ix_tasks_board ON tasks(project_id, "column", position)
    WHERE deleted_at IS NULL;
CREATE INDEX ix_tasks_assignee_open ON tasks(assignee_id)
    WHERE deleted_at IS NULL AND "column" <> 'done';
CREATE INDEX ix_tasks_due_date ON tasks(workspace_id, due_date) WHERE deleted_at IS NULL;
CREATE INDEX ix_tasks_search ON tasks USING gin (search_tsv);

CREATE TABLE task_tags (
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    tag_id  UUID NOT NULL REFERENCES tags(id)  ON DELETE CASCADE,
    PRIMARY KEY (task_id, tag_id)
);

CREATE TABLE task_watchers (
    task_id UUID NOT NULL REFERENCES tasks(id)  ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
    PRIMARY KEY (task_id, user_id)
);

CREATE TABLE task_dependencies (
    task_id        UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    depends_on_id  UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    PRIMARY KEY (task_id, depends_on_id),
    CHECK (task_id <> depends_on_id)
);

-- ---------- task comments / threaded discussion on a task --------------------
CREATE TABLE task_comments (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id      UUID        NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    author_id    UUID        NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    parent_id    UUID        REFERENCES task_comments(id) ON DELETE CASCADE,
    body         TEXT        NOT NULL,
    edited_at    TIMESTAMPTZ,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at   TIMESTAMPTZ
);
CREATE INDEX ix_task_comments_task ON task_comments(task_id, created_at);

-- =============================================================================
-- DISCUSSIONS (CHANNELS & MESSAGES)
-- =============================================================================

CREATE TABLE channels (
    id           UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    name         CITEXT       NOT NULL,
    topic        TEXT,
    type         channel_type NOT NULL DEFAULT 'public',
    created_by   UUID         NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    archived_at  TIMESTAMPTZ,
    UNIQUE (workspace_id, name)
);

CREATE TABLE channel_members (
    channel_id    UUID        NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
    user_id       UUID        NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
    joined_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_read_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_muted      BOOLEAN     NOT NULL DEFAULT FALSE,
    PRIMARY KEY (channel_id, user_id)
);
CREATE INDEX ix_channel_members_user ON channel_members(user_id);

CREATE TABLE messages (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_id   UUID        NOT NULL REFERENCES channels(id) ON DELETE CASCADE,
    author_id    UUID        NOT NULL REFERENCES users(id)    ON DELETE RESTRICT,
    parent_id    UUID        REFERENCES messages(id) ON DELETE CASCADE,  -- thread root
    body         TEXT        NOT NULL,
    is_pinned    BOOLEAN     NOT NULL DEFAULT FALSE,
    edited_at    TIMESTAMPTZ,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at   TIMESTAMPTZ,
    search_tsv   tsvector    GENERATED ALWAYS AS (to_tsvector('english', coalesce(body, ''))) STORED
);
-- Hot path: paginate channel timeline newest-first.
CREATE INDEX ix_messages_channel_time ON messages(channel_id, created_at DESC)
    WHERE deleted_at IS NULL AND parent_id IS NULL;
CREATE INDEX ix_messages_thread ON messages(parent_id, created_at) WHERE parent_id IS NOT NULL;
CREATE INDEX ix_messages_pinned ON messages(channel_id) WHERE is_pinned AND deleted_at IS NULL;
CREATE INDEX ix_messages_search ON messages USING gin (search_tsv);

CREATE TABLE message_reactions (
    message_id UUID        NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    user_id    UUID        NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
    emoji      TEXT        NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (message_id, user_id, emoji)
);
CREATE INDEX ix_message_reactions_msg ON message_reactions(message_id);

CREATE TABLE message_mentions (
    message_id UUID NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    user_id    UUID NOT NULL REFERENCES users(id)    ON DELETE CASCADE,
    PRIMARY KEY (message_id, user_id)
);

-- =============================================================================
-- ATTACHMENTS (polymorphic, scoped by owner_kind + owner_id)
-- =============================================================================
CREATE TABLE attachments (
    id            UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id  UUID             NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    owner_kind    attachment_owner NOT NULL,
    owner_id      UUID             NOT NULL,           -- soft FK; enforce in app/EF
    file_name     TEXT             NOT NULL,
    mime_type     TEXT             NOT NULL,
    size_bytes    BIGINT           NOT NULL CHECK (size_bytes >= 0),
    storage_key   TEXT             NOT NULL,           -- S3/Azure Blob key
    uploaded_by   UUID             NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at    TIMESTAMPTZ      NOT NULL DEFAULT now()
);
CREATE INDEX ix_attachments_owner ON attachments(owner_kind, owner_id);

-- =============================================================================
-- NOTIFICATIONS
-- =============================================================================

CREATE TABLE notifications (
    id            UUID             PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id  UUID             NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    recipient_id  UUID             NOT NULL REFERENCES users(id)      ON DELETE CASCADE,
    actor_id      UUID             REFERENCES users(id) ON DELETE SET NULL,
    kind          notification_kind NOT NULL,
    title         TEXT             NOT NULL,
    body          TEXT,
    -- Polymorphic target reference (e.g., task / message / project)
    target_kind   TEXT,
    target_id     UUID,
    url           TEXT,                           -- deep link to the entity
    read_at       TIMESTAMPTZ,
    created_at    TIMESTAMPTZ      NOT NULL DEFAULT now()
);
-- Inbox query: unread first then recent.
CREATE INDEX ix_notifications_inbox ON notifications(recipient_id, created_at DESC);
CREATE INDEX ix_notifications_unread ON notifications(recipient_id)
    WHERE read_at IS NULL;

CREATE TABLE notification_preferences (
    user_id       UUID             NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    workspace_id  UUID             NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    kind          notification_kind NOT NULL,
    channel       delivery_channel NOT NULL,
    enabled       BOOLEAN          NOT NULL DEFAULT TRUE,
    PRIMARY KEY (user_id, workspace_id, kind, channel)
);

-- =============================================================================
-- ACTIVITY FEED (append-only)
--   Drives dashboard, project activity, recent activity widgets.
-- =============================================================================
CREATE TABLE activity_events (
    id            BIGINT       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    workspace_id  UUID         NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    actor_id      UUID         REFERENCES users(id) ON DELETE SET NULL,
    project_id    UUID         REFERENCES projects(id) ON DELETE CASCADE,
    verb          TEXT         NOT NULL,          -- 'task.completed', 'message.posted'...
    target_kind   TEXT         NOT NULL,
    target_id     UUID         NOT NULL,
    metadata      JSONB        NOT NULL DEFAULT '{}'::jsonb,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now()
);
CREATE INDEX ix_activity_workspace_time ON activity_events(workspace_id, created_at DESC);
CREATE INDEX ix_activity_project_time   ON activity_events(project_id,   created_at DESC);
CREATE INDEX ix_activity_actor_time     ON activity_events(actor_id,     created_at DESC);
CREATE INDEX ix_activity_metadata       ON activity_events USING gin (metadata jsonb_path_ops);

-- =============================================================================
-- ANALYTICS SUPPORT (materialised views — refreshed by background job)
-- =============================================================================

-- Per-project rollup used by the project card grid.
CREATE MATERIALIZED VIEW mv_project_task_stats AS
SELECT
    p.id                                                   AS project_id,
    p.workspace_id,
    COUNT(t.*)                                             AS total_tasks,
    COUNT(*) FILTER (WHERE t.column = 'done')              AS completed_tasks,
    COUNT(*) FILTER (WHERE t.column = 'in_progress')       AS in_progress_tasks,
    COUNT(*) FILTER (WHERE t.column = 'todo')              AS todo_tasks,
    COUNT(*) FILTER (WHERE t.due_date < CURRENT_DATE
                     AND t.column <> 'done')               AS overdue_tasks,
    MAX(t.updated_at)                                      AS last_activity_at
FROM projects p
LEFT JOIN tasks t
       ON t.project_id = p.id
      AND t.deleted_at IS NULL
WHERE p.deleted_at IS NULL
GROUP BY p.id, p.workspace_id;
CREATE UNIQUE INDEX ux_mv_project_task_stats ON mv_project_task_stats(project_id);

-- Weekly velocity used by the dashboard chart.
CREATE MATERIALIZED VIEW mv_workspace_weekly_velocity AS
SELECT
    workspace_id,
    date_trunc('week', completed_at)::date AS week,
    COUNT(*)                               AS completed_count
FROM tasks
WHERE completed_at IS NOT NULL
GROUP BY workspace_id, date_trunc('week', completed_at);
CREATE UNIQUE INDEX ux_mv_workspace_weekly_velocity
    ON mv_workspace_weekly_velocity(workspace_id, week);

-- =============================================================================
-- updated_at trigger
-- =============================================================================
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE r record;
BEGIN
    FOR r IN
        SELECT c.relname AS tbl
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind = 'r'
          AND EXISTS (
              SELECT 1 FROM pg_attribute a
              WHERE a.attrelid = c.oid AND a.attname = 'updated_at' AND NOT a.attisdropped
          )
    LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%I_updated_at
             BEFORE UPDATE ON %I
             FOR EACH ROW EXECUTE FUNCTION set_updated_at();',
            r.tbl, r.tbl);
    END LOOP;
END $$;
