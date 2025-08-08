# **Take-Home Test Submission**

## **Environment Setup & Initial Steps**

1. Forked and cloned the repository locally.
2. Opened and built the solution in Visual Studio to confirm the current project state.
3. **Baseline Quality Check** – Executed the existing automated tests to verify the current functionality and establish a known-good baseline before making any modifications.
4. Set up the Bank Simulator within a Docker container.
5. **Scenario-Based Integration Testing** – Created and executed Postman requests to validate the Bank Simulator’s behaviour against the three primary scenarios:
   * Authorised payment
   * Unauthorised payment
   * Service unavailable

## **Implementation Summary**

### **Controller Enhancements**

* Modified the **GET Payments** controller action to return `NotFound` when a requested payment does not exist, ensuring clear API feedback for missing resources.
* Switched the controller base class to `ControllerBase` since view rendering was not required, reducing unnecessary MVC overhead.
* Updated success responses to use `Ok(...)` instead of `OkObjectResult` for improved readability and clarity.

### **Framework & Structure Improvements**

* Replaced the test framework from xUnit to NUnit, aligning with engineering preferences.
* Moved `PaymentsControllerTests` to a dedicated **Integration** test folder to better reflect test scope and purpose.

### **New Features & Service Layer**

* Implemented the **Create Payment** action with validation of the `PostPaymentRequest` model.
* Introduced a **Payment Service** to handle core payment logic, ensuring new payments are persisted to the repository.
* Added acquiring bank integration logic to facilitate end-to-end payment flow.
* Introduced `IPaymentsRepository` interface to support dependency inversion and enable more testable designs.
* Added unit tests for:
  * `FutureExpiryDateAttribute` validation
  * `PaymentsController` behaviours
  * `AcquiringBankClient` integration logic

### **Developer Experience & Documentation**

* Improved Swagger documentation to make API exploration easier for developers and testers.

## **Design Considerations**

* Ensured that controller logic remains thin by delegating core business operations to dedicated services.
* Applied the **Dependency Inversion Principle (DIP)** to make core components more testable and loosely coupled.
* Used a mix of unit tests and integration tests to verify correctness at both component and system levels.
* Focused on readability and maintainability.

## **Future Enhancements**

* Consult with the development team to address the `PostPaymentRequest` object’s flow across Controller, Service, and Repository layers; consider introducing a dedicated domain model to better encapsulate business logic and improve separation of concerns.
* Add comprehensive logging to facilitate easier troubleshooting and auditability.
* Implement additional exception handling to ensure robustness and graceful failure across all layers.
