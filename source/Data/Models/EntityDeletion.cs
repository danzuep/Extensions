using System.ComponentModel.DataAnnotations;

namespace Common.Data.Models;

public abstract record EntityDeletion
{
    [Display(Name = "Is Deleted")]
    [ScaffoldColumn(false)]
    public bool IsDeleted { get; set; }
}
