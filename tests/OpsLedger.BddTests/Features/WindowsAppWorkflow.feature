Feature: Windows app workflow
  Operators need the installed Windows app to support the same request workflow
  that is validated through the API.

  @ui @windows
  Scenario: Submit a valid request through the Windows app
    Given the OpsLedger Windows app automation run is enabled
    And the published OpsLedger Windows app is available
    When an employee submits a service request through the Windows app
    Then the Windows app shows "Service request created."
