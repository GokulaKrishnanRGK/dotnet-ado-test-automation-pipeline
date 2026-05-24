using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION opsledger_create_service_request(
                    p_id TEXT,
                    p_title TEXT,
                    p_category TEXT,
                    p_priority TEXT,
                    p_description TEXT,
                    p_requester_name TEXT,
                    p_requester_email TEXT,
                    p_impact_details TEXT,
                    p_status TEXT,
                    p_created_at TIMESTAMPTZ,
                    p_sla_due_at TIMESTAMPTZ
                )
                RETURNS VOID
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    INSERT INTO service_requests (
                        id,
                        title,
                        category,
                        priority,
                        description,
                        requester_name,
                        requester_email,
                        impact_details,
                        status,
                        created_at,
                        sla_due_at
                    )
                    VALUES (
                        p_id,
                        p_title,
                        p_category,
                        p_priority,
                        p_description,
                        p_requester_name,
                        p_requester_email,
                        p_impact_details,
                        p_status,
                        p_created_at,
                        p_sla_due_at
                    );

                    INSERT INTO service_request_activity (
                        service_request_id,
                        type,
                        occurred_at,
                        description
                    )
                    VALUES (
                        p_id,
                        'Created',
                        p_created_at,
                        'Service request created.'
                    );
                END;
                $$;

                CREATE OR REPLACE FUNCTION opsledger_assign_service_request(
                    p_id TEXT,
                    p_assignee_name TEXT,
                    p_changed_at TIMESTAMPTZ
                )
                RETURNS VOID
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    UPDATE service_requests
                    SET
                        assignee_name = p_assignee_name,
                        status = 'InProgress'
                    WHERE id = p_id;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'Service request % was not found.', p_id;
                    END IF;

                    INSERT INTO service_request_activity (
                        service_request_id,
                        type,
                        occurred_at,
                        description
                    )
                    VALUES (
                        p_id,
                        'Assigned',
                        p_changed_at,
                        'Assigned to ' || p_assignee_name || '.'
                    );
                END;
                $$;

                CREATE OR REPLACE FUNCTION opsledger_resolve_service_request(
                    p_id TEXT,
                    p_resolution_notes TEXT,
                    p_changed_at TIMESTAMPTZ
                )
                RETURNS VOID
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    UPDATE service_requests
                    SET
                        resolution_notes = p_resolution_notes,
                        status = 'Resolved'
                    WHERE id = p_id;

                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'Service request % was not found.', p_id;
                    END IF;

                    INSERT INTO service_request_activity (
                        service_request_id,
                        type,
                        occurred_at,
                        description
                    )
                    VALUES (
                        p_id,
                        'Resolved',
                        p_changed_at,
                        'Request resolved.'
                    );
                END;
                $$;

                CREATE OR REPLACE FUNCTION opsledger_add_service_request_comment(
                    p_id TEXT,
                    p_author_name TEXT,
                    p_body TEXT,
                    p_created_at TIMESTAMPTZ
                )
                RETURNS VOID
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM service_requests
                        WHERE id = p_id
                    ) THEN
                        RAISE EXCEPTION 'Service request % was not found.', p_id;
                    END IF;

                    INSERT INTO service_request_comments (
                        service_request_id,
                        author_name,
                        body,
                        created_at
                    )
                    VALUES (
                        p_id,
                        p_author_name,
                        p_body,
                        p_created_at
                    );

                    INSERT INTO service_request_activity (
                        service_request_id,
                        type,
                        occurred_at,
                        description
                    )
                    VALUES (
                        p_id,
                        'CommentAdded',
                        p_created_at,
                        'Comment added by ' || p_author_name || '.'
                    );
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS opsledger_add_service_request_comment(TEXT, TEXT, TEXT, TIMESTAMPTZ);
                DROP FUNCTION IF EXISTS opsledger_resolve_service_request(TEXT, TEXT, TIMESTAMPTZ);
                DROP FUNCTION IF EXISTS opsledger_assign_service_request(TEXT, TEXT, TIMESTAMPTZ);
                DROP FUNCTION IF EXISTS opsledger_create_service_request(TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TEXT, TIMESTAMPTZ, TIMESTAMPTZ);
                """);
        }
    }
}
