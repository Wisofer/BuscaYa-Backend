using System.ComponentModel.DataAnnotations.Schema;

namespace BuscaYa.Models.Entities;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Column("id")]
    public int Id { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;

    /// <summary>SHA256 en hexadecimal del token en claro (nunca guardar el token plano).</summary>
    [Column("token_hash")]
    public string TokenHash { get; set; } = "";

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }
}
