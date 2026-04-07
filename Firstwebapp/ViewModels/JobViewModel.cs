using System.ComponentModel.DataAnnotations;

namespace Thekdar.Models;

public class JobViewModel
{
	[Key]
	public int Id { get; set; }

	[Required]
	[StringLength(150)]
	public string Title { get; set; } = null!;

	[StringLength(2000)]
	public string? Description { get; set; }

	[StringLength(100)]
	public string? ClientName { get; set; }

	[StringLength(200)]
	public string? Address { get; set; }

	public DateTime? ScheduledDate { get; set; }

	public bool IsCompleted { get; set; } = false;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
