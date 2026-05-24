CREATE OR REPLACE FUNCTION opsledger_get_service_request(
    p_id TEXT
)
RETURNS SETOF service_requests
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM service_requests
    WHERE id = p_id;
$$;

CREATE OR REPLACE FUNCTION opsledger_list_service_requests(
    p_status TEXT,
    p_priority TEXT
)
RETURNS SETOF service_requests
LANGUAGE sql
STABLE
AS $$
    SELECT *
    FROM service_requests
    WHERE (
        p_status IS NULL OR
        btrim(p_status) = '' OR
        p_status = 'All' OR
        status = btrim(p_status)
    )
    AND (
        p_priority IS NULL OR
        btrim(p_priority) = '' OR
        p_priority = 'All' OR
        priority = btrim(p_priority)
    )
    ORDER BY created_at DESC;
$$;
