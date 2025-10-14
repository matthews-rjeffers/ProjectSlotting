using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectScheduler.Models
{
    public class OnsiteSchedule
    {
        [Key]
        public int OnsiteScheduleId { get; set; }

        public int ProjectId { get; set; }

        [Required]
        public DateTime WeekStartDate { get; set; }

        [Required]
        [Range(1, 20)]
        public int EngineerCount { get; set; }

        [Required]
        [Range(1, 200)]
        public int TotalHours { get; set; } = 40; // Total hours for the onsite week

        [Required]
        [MaxLength(20)]
        public string OnsiteType { get; set; } = "UAT"; // "UAT" or "GoLive"

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
    }
}
