CREATE TABLE IF NOT EXISTS service_requests (
    id               TEXT        NOT NULL,
    title            TEXT        NOT NULL,
    category         TEXT        NOT NULL,
    priority         TEXT        NOT NULL,
    description      TEXT        NOT NULL,
    requester_name   TEXT        NOT NULL,
    requester_email  TEXT        NOT NULL,
    impact_details   TEXT,
    status           TEXT        NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL,
    sla_due_at       TIMESTAMPTZ NOT NULL,
    assignee_name    TEXT,
    resolution_notes TEXT,
    CONSTRAINT pk_service_requests PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS service_request_activity (
    id                 BIGINT      GENERATED ALWAYS AS IDENTITY,
    service_request_id TEXT        NOT NULL,
    type               TEXT        NOT NULL,
    occurred_at        TIMESTAMPTZ NOT NULL,
    description        TEXT        NOT NULL,
    CONSTRAINT pk_service_request_activity PRIMARY KEY (id),
    CONSTRAINT fk_service_request_activity_request
        FOREIGN KEY (service_request_id)
        REFERENCES service_requests (id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS service_request_comments (
    id                 BIGINT      GENERATED ALWAYS AS IDENTITY,
    service_request_id TEXT        NOT NULL,
    author_name        TEXT        NOT NULL,
    body               TEXT        NOT NULL,
    created_at         TIMESTAMPTZ NOT NULL,
    CONSTRAINT pk_service_request_comments PRIMARY KEY (id),
    CONSTRAINT fk_service_request_comments_request
        FOREIGN KEY (service_request_id)
        REFERENCES service_requests (id)
        ON DELETE CASCADE
);
