using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuscaYa.Models.Entities;

[Table("exchange_rate_logs")]
public class ExchangeRateLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("mode")]
    public string Mode { get; set; } = string.Empty;

    [Column("rate", TypeName = "numeric(14,4)")]
    public decimal Rate { get; set; }

    [MaxLength(100)]
    [Column("source")]
    public string? Source { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
