namespace PatientSyncHealth.Middleware.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entityName, string id)
        : base($"{entityName} with ID '{id}' was not found") { }
}
