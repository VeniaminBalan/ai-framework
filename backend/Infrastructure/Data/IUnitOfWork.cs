namespace PatientSyncHealth.Infrastructure.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task InTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
    Task<TReturn> InTransactionAsync<TReturn>(Func<Task<TReturn>> action, CancellationToken cancellationToken = default);
}
