using System;
using KLC.Enums;
using KLC.Faults;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Faults;

/// <summary>
/// Tests for fault business logic exercised by FaultAppService.
/// Validates domain rules for fault lifecycle, priority determination, and status transitions.
/// </summary>
public class FaultAppServiceTests
{
    private static Fault CreateTestFault(
        string errorCode = "ConnectorLockFailure",
        string? errorInfo = null,
        int? connectorNumber = 1)
    {
        return new Fault(
            Guid.NewGuid(),
            Guid.NewGuid(),
            connectorNumber,
            errorCode,
            errorInfo);
    }

    [Fact]
    public void CreateFault_Should_Set_Default_Values()
    {
        var stationId = Guid.NewGuid();

        var fault = new Fault(
            Guid.NewGuid(),
            stationId,
            connectorNumber: 1,
            errorCode: "GroundFailure",
            errorInfo: "Ground fault detected");

        fault.StationId.ShouldBe(stationId);
        fault.ConnectorNumber.ShouldBe(1);
        fault.ErrorCode.ShouldBe("GroundFailure");
        fault.ErrorInfo.ShouldBe("Ground fault detected");
        fault.Status.ShouldBe(FaultStatus.Open);
        fault.DetectedAt.ShouldNotBe(default);
        fault.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void CreateFault_CriticalError_Should_Have_Priority1()
    {
        var criticalCodes = new[]
        {
            "GroundFailure", "HighTemperature", "OverCurrentFailure",
            "OverVoltage", "UnderVoltage", "PowerMeterFailure"
        };

        foreach (var code in criticalCodes)
        {
            var fault = CreateTestFault(errorCode: code);
            fault.Priority.ShouldBe(1, $"Error code '{code}' should be priority 1 (critical)");
        }
    }

    [Fact]
    public void CreateFault_HighPriorityError_Should_Have_Priority2()
    {
        var highCodes = new[]
        {
            "ConnectorLockFailure", "EVCommunicationError",
            "ReaderFailure", "InternalError"
        };

        foreach (var code in highCodes)
        {
            var fault = CreateTestFault(errorCode: code);
            fault.Priority.ShouldBe(2, $"Error code '{code}' should be priority 2 (high)");
        }
    }

    [Fact]
    public void CreateFault_UnknownError_Should_Have_Priority3()
    {
        var fault = CreateTestFault(errorCode: "SomeOtherError");

        fault.Priority.ShouldBe(3); // Default medium priority
    }

    [Fact]
    public void StartInvestigation_From_Open_Should_Succeed()
    {
        var fault = CreateTestFault();

        fault.StartInvestigation();

        fault.Status.ShouldBe(FaultStatus.Investigating);
    }

    [Fact]
    public void StartInvestigation_From_NonOpen_Should_Throw()
    {
        var fault = CreateTestFault();
        fault.StartInvestigation();

        // Already investigating, cannot start again
        var ex = Should.Throw<BusinessException>(() =>
            fault.StartInvestigation());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fault.InvalidStatusTransition);
    }

    [Fact]
    public void Resolve_Should_Set_Resolved_Status_And_Details()
    {
        var fault = CreateTestFault();
        var resolvedBy = Guid.NewGuid();

        fault.Resolve(resolvedBy, "Connector replaced");

        fault.Status.ShouldBe(FaultStatus.Resolved);
        fault.ResolvedAt.ShouldNotBeNull();
        fault.ResolvedByUserId.ShouldBe(resolvedBy);
        fault.ResolutionNotes.ShouldBe("Connector replaced");
    }

    [Fact]
    public void Close_Should_Set_Closed_Status()
    {
        var fault = CreateTestFault();

        fault.Close("False alarm — no issue found");

        fault.Status.ShouldBe(FaultStatus.Closed);
        fault.ResolvedAt.ShouldNotBeNull();
        fault.ResolutionNotes.ShouldBe("False alarm — no issue found");
    }

    [Fact]
    public void Reopen_Resolved_Fault_Should_Clear_Resolution()
    {
        var fault = CreateTestFault();
        fault.Resolve(Guid.NewGuid(), "Fixed");

        fault.Reopen();

        fault.Status.ShouldBe(FaultStatus.Open);
        fault.ResolvedAt.ShouldBeNull();
        fault.ResolvedByUserId.ShouldBeNull();
        fault.ResolutionNotes.ShouldBeNull();
    }

    [Fact]
    public void Reopen_Open_Fault_Should_Throw()
    {
        var fault = CreateTestFault();

        var ex = Should.Throw<BusinessException>(() => fault.Reopen());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fault.InvalidStatusTransition);
    }

    [Fact]
    public void SetPriority_Valid_Range_Should_Succeed()
    {
        var fault = CreateTestFault();

        fault.SetPriority(1);
        fault.Priority.ShouldBe(1);

        fault.SetPriority(4);
        fault.Priority.ShouldBe(4);
    }

    [Fact]
    public void SetPriority_OutOfRange_Should_Throw()
    {
        var fault = CreateTestFault();

        var ex = Should.Throw<BusinessException>(() => fault.SetPriority(0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fault.InvalidPriority);

        ex = Should.Throw<BusinessException>(() => fault.SetPriority(5));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fault.InvalidPriority);
    }

    [Fact]
    public void UpdateStatus_To_Investigating_Should_Transition()
    {
        var fault = CreateTestFault();

        fault.UpdateStatus(FaultStatus.Investigating);

        fault.Status.ShouldBe(FaultStatus.Investigating);
    }

    [Fact]
    public void UpdateStatus_To_Resolved_Should_Set_ResolvedAt()
    {
        var fault = CreateTestFault();

        fault.UpdateStatus(FaultStatus.Resolved, "Auto-resolved");

        fault.Status.ShouldBe(FaultStatus.Resolved);
        fault.ResolvedAt.ShouldNotBeNull();
        fault.ResolutionNotes.ShouldBe("Auto-resolved");
    }

    [Fact]
    public void UpdateStatus_Reopen_From_Resolved_Should_Work()
    {
        var fault = CreateTestFault();
        fault.Resolve(Guid.NewGuid(), "Fixed");

        fault.UpdateStatus(FaultStatus.Open);

        fault.Status.ShouldBe(FaultStatus.Open);
        fault.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void CreateFault_StationLevel_Should_Have_Null_ConnectorNumber()
    {
        var fault = new Fault(
            Guid.NewGuid(),
            Guid.NewGuid(),
            connectorNumber: null,
            errorCode: "InternalError");

        fault.ConnectorNumber.ShouldBeNull();
    }

    [Fact]
    public void CreateFault_With_VendorErrorCode_Should_Preserve()
    {
        var fault = new Fault(
            Guid.NewGuid(),
            Guid.NewGuid(),
            connectorNumber: 1,
            errorCode: "EVCommunicationError",
            errorInfo: "Communication timeout",
            vendorErrorCode: "JUHANG_E102");

        fault.VendorErrorCode.ShouldBe("JUHANG_E102");
        fault.ErrorInfo.ShouldBe("Communication timeout");
    }
}
