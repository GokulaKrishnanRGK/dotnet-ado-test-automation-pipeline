Feature: Service request workflow
  Operators need submitted service requests to appear in the queue
  and invalid requests to be rejected before work begins.

  @api
  Scenario: Submit a valid request and see it in the queue
    Given the OpsLedger API is available
    When an employee submits a "High" priority "Facilities" request titled "Replace conference room display"
    Then the request is accepted
    And the request appears in the queue with status "New"

  @api
  Scenario: Reject a critical request without impact details
    Given the OpsLedger API is available
    When an employee submits a critical request without impact details
    Then the request is rejected with "Critical requests require impact details."
