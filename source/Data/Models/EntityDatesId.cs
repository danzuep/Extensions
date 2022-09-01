using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Data.Models;

/// <summary>
/// Base model class to handle generic table fields
/// </summary>
public abstract record EntityIdDates : EntityDates
{
    [Key]
    [Display(Name = "ID")]
    [ScaffoldColumn(false)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column(TypeName = "varchar(36)")]
    public string Uid { get; set; }
}
