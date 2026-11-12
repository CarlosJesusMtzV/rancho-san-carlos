namespace TecNM.Api.Core.Entities;

/// <summary>
/// Columnas de auditoría presentes en todas las tablas de database.sql.
/// Borrar es marcar <see cref="DeletedAt"/>; el DbContext filtra esos registros.
/// </summary>
public abstract class AuditableEntity
{
    public short Status { get; set; } = 1;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
