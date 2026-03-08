using System.Text.Json;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Ocpp.Messages;
using Shouldly;
using Xunit;

namespace KLC.EntityFrameworkCore.Ocpp;

public class OcppV16MessageParserTests
{
    private readonly OcppV16MessageParser _parser = new();

    #region Parse — Call Messages

    [Fact]
    public void Parse_Call_Should_Return_Action_And_Payload()
    {
        var raw = """[2,"abc123","BootNotification",{"chargePointVendor":"TestVendor","chargePointModel":"TestModel"}]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe(OcppMessageType.Call);
        result.UniqueId.ShouldBe("abc123");
        result.Action.ShouldBe("BootNotification");
        result.Payload.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public void Parse_Call_Should_Handle_Heartbeat_Without_Payload()
    {
        var raw = """[2,"hb001","Heartbeat",{}]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.Action.ShouldBe("Heartbeat");
        result.Payload.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public void Parse_Call_Should_Handle_MeterValues()
    {
        var raw = """[2,"mv001","MeterValues",{"connectorId":1,"transactionId":12345,"meterValue":[{"timestamp":"2026-03-08T10:00:00Z","sampledValue":[{"value":"5000","measurand":"Energy.Active.Import.Register","unit":"Wh"}]}]}]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.Action.ShouldBe("MeterValues");
        result.Payload.GetProperty("connectorId").GetInt32().ShouldBe(1);
        result.Payload.GetProperty("transactionId").GetInt32().ShouldBe(12345);
    }

    #endregion

    #region Parse — CallResult Messages

    [Fact]
    public void Parse_CallResult_Should_Return_Payload()
    {
        var raw = """[3,"abc123",{"status":"Accepted"}]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe(OcppMessageType.CallResult);
        result.UniqueId.ShouldBe("abc123");
        result.Action.ShouldBeNull();
        result.Payload.GetProperty("status").GetString().ShouldBe("Accepted");
    }

    [Fact]
    public void Parse_CallResult_Should_Handle_Empty_Payload()
    {
        var raw = """[3,"abc123",{}]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe(OcppMessageType.CallResult);
        result.Payload.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    #endregion

    #region Parse — CallError Messages

    [Fact]
    public void Parse_CallError_Should_Return_Error_Info()
    {
        var raw = """[4,"abc123","NotImplemented","Action not supported",{}]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe(OcppMessageType.CallError);
        result.UniqueId.ShouldBe("abc123");
        result.ErrorCode.ShouldBe("NotImplemented");
        result.ErrorDescription.ShouldBe("Action not supported");
    }

    [Fact]
    public void Parse_CallError_Should_Handle_Missing_Description()
    {
        var raw = """[4,"abc123","InternalError"]""";

        var result = _parser.Parse(raw);

        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe(OcppMessageType.CallError);
        result.ErrorCode.ShouldBe("InternalError");
    }

    #endregion

    #region Parse — Edge Cases

    [Fact]
    public void Parse_Should_Return_Null_For_Too_Short_Array()
    {
        var raw = """[2,"abc"]""";

        var result = _parser.Parse(raw);

        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_Should_Return_Null_For_Empty_Array()
    {
        var raw = """[]""";

        var result = _parser.Parse(raw);

        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_Should_Return_Null_For_Unknown_MessageType()
    {
        var raw = """[9,"abc123","Unknown",{}]""";

        var result = _parser.Parse(raw);

        result.ShouldBeNull();
    }

    [Fact]
    public void Parse_Should_Throw_For_Invalid_Json()
    {
        Should.Throw<JsonException>(() => _parser.Parse("not json"));
    }

    #endregion

    #region Serialize — CallResult

    [Fact]
    public void SerializeCallResult_Should_Produce_Valid_Json_Array()
    {
        var result = _parser.SerializeCallResult("uid1", new { status = "Accepted", currentTime = "2026-03-08T10:00:00Z", interval = 60 });

        var parsed = JsonSerializer.Deserialize<JsonElement[]>(result);
        parsed.ShouldNotBeNull();
        parsed!.Length.ShouldBe(3);
        parsed[0].GetInt32().ShouldBe(OcppMessageType.CallResult);
        parsed[1].GetString().ShouldBe("uid1");
        parsed[2].GetProperty("status").GetString().ShouldBe("Accepted");
    }

    #endregion

    #region Serialize — CallError

    [Fact]
    public void SerializeCallError_Should_Produce_Valid_Json_Array()
    {
        var result = _parser.SerializeCallError("uid2", "NotImplemented", "Action not supported");

        var parsed = JsonSerializer.Deserialize<JsonElement[]>(result);
        parsed.ShouldNotBeNull();
        parsed!.Length.ShouldBe(5);
        parsed[0].GetInt32().ShouldBe(OcppMessageType.CallError);
        parsed[1].GetString().ShouldBe("uid2");
        parsed[2].GetString().ShouldBe("NotImplemented");
        parsed[3].GetString().ShouldBe("Action not supported");
    }

    #endregion

    #region Serialize — Call

    [Fact]
    public void SerializeCall_Should_Produce_Valid_Json_Array()
    {
        var result = _parser.SerializeCall("uid3", "RemoteStartTransaction", new { idTag = "USER123", connectorId = 1 });

        var parsed = JsonSerializer.Deserialize<JsonElement[]>(result);
        parsed.ShouldNotBeNull();
        parsed!.Length.ShouldBe(4);
        parsed[0].GetInt32().ShouldBe(OcppMessageType.Call);
        parsed[1].GetString().ShouldBe("uid3");
        parsed[2].GetString().ShouldBe("RemoteStartTransaction");
        parsed[3].GetProperty("idTag").GetString().ShouldBe("USER123");
    }

    #endregion

    #region Parser Factory

    [Fact]
    public void Factory_Should_Return_V16_Parser()
    {
        var factory = new OcppMessageParserFactory();
        var parser = factory.GetParser(OcppProtocolVersion.Ocpp16J);

        parser.ShouldNotBeNull();
        parser.Version.ShouldBe(OcppProtocolVersion.Ocpp16J);
        parser.ShouldBeOfType<OcppV16MessageParser>();
    }

    [Fact]
    public void Factory_Should_Throw_For_Ocpp201()
    {
        var factory = new OcppMessageParserFactory();

        Should.Throw<System.NotSupportedException>(() => factory.GetParser(OcppProtocolVersion.Ocpp201));
    }

    #endregion

    #region Roundtrip

    [Fact]
    public void Roundtrip_Call_Should_Parse_Back()
    {
        var serialized = _parser.SerializeCall("rt1", "Heartbeat", new { });
        var parsed = _parser.Parse(serialized);

        parsed.ShouldNotBeNull();
        parsed!.MessageType.ShouldBe(OcppMessageType.Call);
        parsed.UniqueId.ShouldBe("rt1");
        parsed.Action.ShouldBe("Heartbeat");
    }

    [Fact]
    public void Roundtrip_CallResult_Should_Parse_Back()
    {
        var serialized = _parser.SerializeCallResult("rt2", new { currentTime = "2026-03-08T10:00:00Z" });
        var parsed = _parser.Parse(serialized);

        parsed.ShouldNotBeNull();
        parsed!.MessageType.ShouldBe(OcppMessageType.CallResult);
        parsed.UniqueId.ShouldBe("rt2");
    }

    [Fact]
    public void Roundtrip_CallError_Should_Parse_Back()
    {
        var serialized = _parser.SerializeCallError("rt3", "GenericError", "Something went wrong");
        var parsed = _parser.Parse(serialized);

        parsed.ShouldNotBeNull();
        parsed!.MessageType.ShouldBe(OcppMessageType.CallError);
        parsed.UniqueId.ShouldBe("rt3");
        parsed.ErrorCode.ShouldBe("GenericError");
        parsed.ErrorDescription.ShouldBe("Something went wrong");
    }

    #endregion
}
