Feature: Windows app workflow
  Operators need the installed Windows app to support the same request workflow
  that is validated through the API.

  @ui @windows
  Scenario: Submit a valid request through the Windows app
    Given the OpsLedger Windows app automation run is enabled
    And the published OpsLedger Windows app is available
    When an employee submits a service request through the Windows app
    Then the Windows app shows "Service request created."

  @ui @windows
  Scenario: Submit a critical request with impact details through the Windows app
    Given the OpsLedger Windows app automation run is enabled
    And the published OpsLedger Windows app is available
    When an employee submits a critical service request with impact details through the Windows app
    Then the Windows app shows "Service request created."

  @ui @windows
  Scenario: Reject a Windows app request without a title
    Given the OpsLedger Windows app automation run is enabled
    And the published OpsLedger Windows app is available
    When an employee submits a service request without a title through the Windows app
    Then the Windows app shows "Title is required."

  @ui @windows
  Scenario: Reject a Windows app critical request without impact details
    Given the OpsLedger Windows app automation run is enabled
    And the published OpsLedger Windows app is available
    When an employee submits a critical service request without impact details through the Windows app
    Then the Windows app shows "Critical requests require impact details."
