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

  @api
  Scenario: Reject a request with missing title and invalid requester email
    Given the OpsLedger API is available
    When an employee submits a request with a missing title and invalid requester email
    Then the request is rejected with "Title is required."
    And the request is rejected with "Requester email must be a valid email address."

  @api
  Scenario: Assign a request and move it to In Progress
    Given the OpsLedger API is available
    And an employee submitted a "Normal" priority "IT" request titled "Install accounting software"
    When an operator assigns the request to "Morgan Lee"
    Then the request status is "InProgress"
    And the request assignee is "Morgan Lee"

  @api
  Scenario: Block resolving a request without resolution notes
    Given the OpsLedger API is available
    And an employee submitted a "Normal" priority "Facilities" request titled "Repair badge reader"
    When an operator resolves the request without resolution notes
    Then the request is rejected with "Resolution notes are required."

  @api
  Scenario: Resolve a request and update its status
    Given the OpsLedger API is available
    And an employee submitted a "High" priority "Security" request titled "Review access exception"
    When an operator resolves the request with "Access exception reviewed and closed."
    Then the request status is "Resolved"
    And the request resolution notes are "Access exception reviewed and closed."

  @api
  Scenario: Add a comment to a request
    Given the OpsLedger API is available
    And an employee submitted a "Normal" priority "IT" request titled "Replace docking station"
    When an operator adds the comment "Waiting on replacement hardware."
    Then the request contains a comment from "Morgan Lee" saying "Waiting on replacement hardware."
    And the request activity includes "CommentAdded"

  @api
  Scenario: Reject an empty comment
    Given the OpsLedger API is available
    And an employee submitted a "Normal" priority "Facilities" request titled "Repair loose desk leg"
    When an operator adds an empty comment
    Then the request is rejected with "Comment body is required."

  @api
  Scenario: Filter the queue by priority and status
    Given the OpsLedger API is available
    And an employee submitted a "High" priority "IT" request titled "Replace VPN token"
    And an employee submitted a "Low" priority "IT" request titled "Update desk phone label"
    When an operator filters the queue by "High" priority and "New" status
    Then the queue includes "Replace VPN token"
    And the queue does not include "Update desk phone label"
