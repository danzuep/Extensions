using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Common.Data.Models;

/// <summary>
/// Base model class to handle generic table fields
/// </summary>
public abstract record EntityUidDates : EntityDates
{
    [Key]
    [Display(Name = "UID")]
    [Column("Uid")]
    [Unicode(false)]
    [StringLength(36)]
    public string Uid { get; set; }
}
