namespace KLC.Ocpp;

public class OcppCommandResultDto
{
    public string Status { get; set; }

    public string Message { get; set; }

    public object Data { get; set; }

    /// <summary>
    /// Computed property: Returns true if Status == "Accepted"
    /// </summary>
    public bool Success
    {
        get { return Status == "Accepted"; }
    }
}
