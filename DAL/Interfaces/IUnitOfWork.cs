// DAL/Interfaces/IUnitOfWork.cs
using DAL.Repositories.Interfaces;
using DAL.Repositories.Interfaces.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.Interfaces
{
    /// <summary>
    /// Unit of Work ������� ��� ���������� ������������ � �������������
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // ===========================
        // �������� ���������
        // ===========================
        /// <summary>
        /// ������ � DbContext
        /// </summary>
        DbContext Context { get; }

        // ===========================
        // ������������������ �����������
        // ===========================
        /// <summary>
        /// ����������� ��� ������ � ��������������
        /// </summary>
        IUserRepository Users { get; }

        /// <summary>
        /// ����������� ��� ������ � �������� �������������
        /// </summary>
        IUserSessionRepository UserSessions { get; }

        /// <summary>
        /// ����������� ��� ������ � ������ ����������
        /// </summary>
        IActivityLogRepository ActivityLogs { get; }

        // ===========================
        // GENERIC REPOSITORY
        // ===========================
        /// <summary>
        /// �������� generic ����������� ��� ����� ��������
        /// </summary>
        /// <typeparam name="TEntity">��� ��������</typeparam>
        /// <returns>����������� ��� ������ � ���������</returns>
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        // ===========================
        // ���������� ���������
        // ===========================
        /// <summary>
        /// ���������� ���������� ���������
        /// </summary>
        /// <returns>���������� ���������� �������</returns>
        int SaveChanges();

        /// <summary>
        /// ����������� ���������� ���������
        /// </summary>
        /// <param name="cancellationToken">����� ������</param>
        /// <returns>���������� ���������� �������</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // ===========================
        // ���������� - ���������� ������
        // ===========================
        /// <summary>
        /// �������� ����� ���������� (���������� �����)
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// ������������ ������� ���������� (���������� �����)
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// ���������� ������� ���������� (���������� �����)
        /// </summary>
        Task RollbackTransactionAsync();

        // ===========================
        // ���������� - ����������� ������
        // ===========================
        /// <summary>
        /// �������� ���������� � ���������� ������ ����������
        /// </summary>
        /// <param name="cancellationToken">����� ������</param>
        /// <returns>������ ����������</returns>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// ��������� �������� � ���������� � �������������� commit/rollback
        /// </summary>
        /// <param name="action">�������� ��� ����������</param>
        /// <param name="cancellationToken">����� ������</param>
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// ��������� �������� � ���������� � ������������ ����������
        /// </summary>
        /// <typeparam name="T">��� ������������� ����������</typeparam>
        /// <param name="action">�������� ��� ����������</param>
        /// <param name="cancellationToken">����� ������</param>
        /// <returns>��������� ���������� ��������</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

        // ===========================
        // SQL �������
        // ===========================
        /// <summary>
        /// ��������� ����� SQL ������
        /// </summary>
        /// <param name="sql">SQL ������</param>
        /// <param name="cancellationToken">����� ������</param>
        /// <returns>���������� ���������� �������</returns>
        Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default);

        /// <summary>
        /// ��������� SQL ������ � �����������
        /// </summary>
        /// <param name="sql">SQL ������</param>
        /// <param name="parameters">��������� �������</param>
        /// <returns>���������� ���������� �������</returns>
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);

        // ===========================
        // ���������� ����������
        // ===========================
        /// <summary>
        /// ����������� ��� �������� �� ���������
        /// </summary>
        void DetachAllEntities();

        /// <summary>
        /// ���������� ������������� ��� ������� (PostgreSQL ������)
        /// </summary>
        /// <typeparam name="TEntity">��� ��������</typeparam>
        Task ResetSequenceAsync<TEntity>() where TEntity : class;

        /// <summary>
        /// ���������, ���� �� ������������� ���������
        /// </summary>
        /// <returns>True, ���� ���� ������������� ���������</returns>
        bool HasUnsavedChanges();

        /// <summary>
        /// �������� ��� ��������� � ���������
        /// </summary>
        void RejectChanges();

        // ===========================
        // �������������� ������
        // ===========================
        /// <summary>
        /// ��������� ����������� ���� ������
        /// </summary>
        /// <returns>True, ���� ���� ������ ��������</returns>
        Task<bool> CanConnectAsync();

        /// <summary>
        /// �������� ���������� � ��������� �����������
        /// </summary>
        /// <returns>��������� �����������</returns>
        string GetConnectionState();

        /// <summary>
        /// ��������� �������� ���� ������
        /// </summary>
        Task MigrateAsync();

        /// <summary>
        /// �������� ������ ��������� ��������
        /// </summary>
        /// <returns>������ ��������</returns>
        Task<IEnumerable<string>> GetPendingMigrationsAsync();

        /// <summary>
        /// �������� ������ ����������� ��������
        /// </summary>
        /// <returns>������ ��������</returns>
        Task<IEnumerable<string>> GetAppliedMigrationsAsync();
    }
}