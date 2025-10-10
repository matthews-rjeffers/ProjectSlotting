using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectScheduler.Models;

namespace ProjectScheduler.Services
{
    public interface IAllocationService
    {
        Task<bool> AllocateProjectToSquad(int projectId, int squadId, DateTime startDate, DateTime crpDate, decimal totalDevHours);
        Task<bool> ReallocateProject(int projectId, int squadId, DateTime newStartDate);
        Task RemoveProjectAllocations(int projectId, int? squadId = null);
        Task<List<ProjectAllocation>> GetProjectAllocations(int projectId);
        Task<bool> CanAllocateProject(int squadId, DateTime startDate, DateTime crpDate, decimal totalDevHours);
        Task<AllocationPreview> PreviewProjectAllocation(int projectId, int squadId);
    }

    public class AllocationPreview
    {
        public bool CanAllocate { get; set; }
        public string? Message { get; set; }
        public List<WeeklyCapacityPreview> WeeklyCapacity { get; set; } = new();
    }

    public class WeeklyCapacityPreview
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public decimal CurrentDevHours { get; set; }
        public decimal CurrentOnsiteHours { get; set; }
        public decimal PreviewDevHours { get; set; }
        public decimal PreviewOnsiteHours { get; set; }
        public decimal TotalCapacity { get; set; }
        public decimal CurrentUtilization { get; set; }
        public decimal PreviewUtilization { get; set; }
        public bool WouldExceedCapacity { get; set; }
    }
}
